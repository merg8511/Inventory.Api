using System.Security.Claims;
using Inventory.Application.Common.Interfaces;

namespace Inventory.Api.Middleware;

public class TenantMiddleware
{
    private const string TenantClaimType = "tenant_id";
    private readonly RequestDelegate _next;
    
    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, ICurrentTenantService tenantService, ILogger<TenantMiddleware> logger)
    {
        // Skip tenant resolution for health checks and dev endpoints
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/dev") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }
        
        var isAuthenticated = context.User?.Identity?.IsAuthenticated ?? false;
        var tenantIdClaim = context.User?.FindFirst(TenantClaimType)?.Value;
        
        logger.LogDebug(
            "TenantMiddleware: IsAuthenticated={IsAuthenticated}, TenantClaim={TenantClaim}, Path={Path}",
            isAuthenticated, 
            tenantIdClaim ?? "(null)", 
            context.Request.Path);
        
        if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            tenantService.SetTenant(tenantId);
            logger.LogDebug("TenantMiddleware: Set TenantId={TenantId}", tenantId);
        }
        else if (isAuthenticated)
        {
            // For development: use a default tenant if authenticated but no tenant claim
            var defaultTenant = Guid.Parse("00000000-0000-0000-0000-000000000001");
            tenantService.SetTenant(defaultTenant);
            logger.LogWarning(
                "TenantMiddleware: No tenant_id claim found, using default tenant {TenantId}", 
                defaultTenant);
        }
        
        await _next(context);
    }
}
