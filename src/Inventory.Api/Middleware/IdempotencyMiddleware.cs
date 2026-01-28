using System.Text.Json;
using Inventory.Infrastructure.Services;

namespace Inventory.Api.Middleware;

public class IdempotencyMiddleware
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private readonly RequestDelegate _next;
    
    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, IIdempotencyService idempotencyService)
    {
        // Only apply to POST, PUT, PATCH methods
        if (!HttpMethods.IsPost(context.Request.Method) && 
            !HttpMethods.IsPut(context.Request.Method) && 
            !HttpMethods.IsPatch(context.Request.Method))
        {
            await _next(context);
            return;
        }
        
        var idempotencyKey = context.Request.Headers[IdempotencyKeyHeader].FirstOrDefault();
        
        if (string.IsNullOrEmpty(idempotencyKey))
        {
            await _next(context);
            return;
        }
        
        // Check if we already processed this key
        var existing = await idempotencyService.GetAsync(idempotencyKey);
        
        if (existing is not null)
        {
            // Return cached response
            context.Response.StatusCode = existing.ResponseStatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(existing.ResponseBody);
            return;
        }
        
        // Store the key in HttpContext for the endpoint to use
        context.Items["IdempotencyKey"] = idempotencyKey;
        
        // Enable response buffering to capture response
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        
        await _next(context);
        
        // Read the response
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();
        responseBody.Seek(0, SeekOrigin.Begin);
        
        // Save idempotency key with response for successful operations
        if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
        {
            context.Request.Body.Seek(0, SeekOrigin.Begin);
            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var requestHash = await idempotencyService.ComputeHashAsync(requestBody);
            
            try
            {
                await idempotencyService.SaveAsync(
                    idempotencyKey, 
                    requestHash, 
                    context.Response.StatusCode, 
                    responseText);
            }
            catch
            {
                // Ignore save errors - idempotency is best effort
            }
        }
        
        // Copy to original stream
        await responseBody.CopyToAsync(originalBodyStream);
    }
}
