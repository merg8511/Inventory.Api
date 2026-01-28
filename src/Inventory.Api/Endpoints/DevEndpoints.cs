using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Inventory.Api.Endpoints;

public static class DevEndpoints
{
    private const string DevSecret = "ThisIsADevelopmentSecretKeyThatIsAtLeast256BitsLong!";
    
    public static void MapDevEndpoints(this WebApplication app)
    {
        // Only register in Development environment
        if (!app.Environment.IsDevelopment())
            return;
            
        var group = app.MapGroup("/dev")
            .WithTags("Development")
            .AllowAnonymous();
            
        group.MapPost("/token", GenerateToken)
            .WithName("GenerateDevToken")
            .WithSummary("Generate a JWT token for development testing");
            
        group.MapGet("/token/admin", () => GenerateTokenInternal("admin", true))
            .WithName("GenerateAdminToken")
            .WithSummary("Quick admin token with all permissions");
            
        group.MapGet("/token/readonly", () => GenerateTokenInternal("reader", false))
            .WithName("GenerateReadOnlyToken")
            .WithSummary("Quick read-only token");
    }
    
    private static IResult GenerateToken(TokenRequest request)
    {
        var token = GenerateJwtToken(
            request.UserId ?? "dev-user",
            request.UserName ?? "Developer",
            request.TenantId ?? "00000000-0000-0000-0000-000000000001",
            request.Scopes ?? ["Inventory.Read", "Inventory.Write", "Items.Manage", "Warehouses.Manage", "Reports.Read"],
            request.ExpiresInMinutes ?? 60
        );
        
        return Results.Ok(new TokenResponse(token, request.ExpiresInMinutes ?? 60));
    }
    
    private static IResult GenerateTokenInternal(string userId, bool isAdmin)
    {
        var scopes = isAdmin 
            ? new[] { "Inventory.Read", "Inventory.Write", "Items.Manage", "Warehouses.Manage", "Reports.Read" }
            : new[] { "Inventory.Read", "Reports.Read" };
            
        var token = GenerateJwtToken(
            userId,
            isAdmin ? "Admin User" : "Read-Only User",
            "00000000-0000-0000-0000-000000000001",
            scopes,
            60
        );
        
        return Results.Ok(new TokenResponse(token, 60));
    }
    
    private static string GenerateJwtToken(
        string userId, 
        string userName, 
        string tenantId, 
        string[] scopes,
        int expiresInMinutes)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DevSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Name, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", tenantId),
            new("scope", string.Join(" ", scopes))
        };
        
        // Also add individual scope claims for policy checks
        foreach (var scope in scopes)
        {
            claims.Add(new Claim("scp", scope));
        }
        
        var token = new JwtSecurityToken(
            issuer: "inventory-api-dev",
            audience: "inventory-api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record TokenRequest(
    string? UserId = null,
    string? UserName = null,
    string? TenantId = null,
    string[]? Scopes = null,
    int? ExpiresInMinutes = null
);

public record TokenResponse(string Token, int ExpiresInMinutes)
{
    public string TokenType => "Bearer";
    public string Usage => "Add to request header: Authorization: Bearer {Token}";
}
