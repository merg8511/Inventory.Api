using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using Inventory.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
    
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? "";
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        
        var (statusCode, errorCode, title, detail, errors) = exception switch
        {
            DomainException domainEx => (
                domainEx is InsufficientStockException ? 400 :
                domainEx is ConcurrencyConflictException ? 409 : 400,
                domainEx.ErrorCode,
                GetTitle(domainEx.ErrorCode),
                domainEx.Message,
                Array.Empty<ValidationErrorDetail>()
            ),
            ValidationException validationEx => (
                400,
                "VALIDATION_ERROR",
                "Validation Error",
                "One or more validation errors occurred.",
                validationEx.Errors.Select(e => new ValidationErrorDetail(e.PropertyName, e.ErrorMessage)).ToArray()
            ),
            UnauthorizedAccessException => (
                401,
                "UNAUTHORIZED",
                "Unauthorized",
                "Authentication is required to access this resource.",
                Array.Empty<ValidationErrorDetail>()
            ),
            _ => (
                500,
                "INTERNAL_ERROR",
                "Internal Server Error",
                "An unexpected error occurred.",
                Array.Empty<ValidationErrorDetail>()
            )
        };
        
        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}, CorrelationId: {CorrelationId}", 
                traceId, correlationId);
        }
        else
        {
            _logger.LogWarning("Business error occurred: {ErrorCode} - {Detail}", errorCode, detail);
        }
        
        var problemDetails = new ProblemDetails
        {
            Type = $"https://inventory.api/errors/{errorCode.ToLowerInvariant().Replace('_', '-')}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };
        
        problemDetails.Extensions["errorCode"] = errorCode;
        problemDetails.Extensions["traceId"] = traceId;
        problemDetails.Extensions["correlationId"] = correlationId;
        
        if (errors.Length > 0)
        {
            problemDetails.Extensions["errors"] = errors;
        }
        
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        
        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await context.Response.WriteAsync(json);
    }
    
    private static string GetTitle(string errorCode) => errorCode switch
    {
        "INSUFFICIENT_STOCK" => "Insufficient Stock",
        "CONCURRENCY_CONFLICT" => "Concurrency Conflict",
        "ITEM_NOT_FOUND" => "Item Not Found",
        "WAREHOUSE_NOT_FOUND" => "Warehouse Not Found",
        "TRANSFER_NOT_FOUND" => "Transfer Not Found",
        "RESERVATION_NOT_FOUND" => "Reservation Not Found",
        "INVALID_STATUS" => "Invalid Status",
        "SKU_ALREADY_EXISTS" => "SKU Already Exists",
        _ => "Error"
    };
}

public record ValidationErrorDetail(string Field, string Message);
