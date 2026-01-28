using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Inventory.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSwaggerWithAuth(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Inventory API",
                Version = "v1",
                Description = "Production-ready Inventory Management API with ledger-based stock tracking",
                Contact = new OpenApiContact
                {
                    Name = "API Support",
                    Email = "support@example.com"
                }
            });
            
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer {token}'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        
        return services;
    }
    
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        const string DevSecret = "ThisIsADevelopmentSecretKeyThatIsAtLeast256BitsLong!";
        var validateIssuer = configuration.GetValue("Jwt:ValidateIssuer", true);
        var validateAudience = configuration.GetValue("Jwt:ValidateAudience", true);
        var isDevelopment = !validateIssuer && !validateAudience;
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (isDevelopment)
                {
                    // Development mode: use symmetric key for dev tokens
                    // MapInboundClaims = false prevents ASP.NET from remapping claims
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(DevSecret))
                    };
                }
                else
                {
                    // Production mode: use external identity provider
                    options.Authority = configuration["Jwt:Authority"];
                    options.Audience = configuration["Jwt:Audience"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = validateIssuer,
                        ValidateAudience = validateAudience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true
                    };
                }
            });
            
        return services;
    }
    
    public static IServiceCollection AddAuthorizationWithPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("InventoryRead", policy => 
                policy.RequireAssertion(context =>
                    context.User.HasClaim("scope", "Inventory.Read") ||
                    context.User.HasClaim("scp", "Inventory.Read") ||
                    context.User.Identity?.IsAuthenticated == true)); // Dev fallback
                    
            options.AddPolicy("InventoryWrite", policy => 
                policy.RequireAssertion(context =>
                    context.User.HasClaim("scope", "Inventory.Write") ||
                    context.User.HasClaim("scp", "Inventory.Write") ||
                    context.User.Identity?.IsAuthenticated == true));
                    
            options.AddPolicy("ItemsManage", policy => 
                policy.RequireAssertion(context =>
                    context.User.HasClaim("scope", "Items.Manage") ||
                    context.User.HasClaim("scp", "Items.Manage") ||
                    context.User.Identity?.IsAuthenticated == true));
                    
            options.AddPolicy("WarehousesManage", policy => 
                policy.RequireAssertion(context =>
                    context.User.HasClaim("scope", "Warehouses.Manage") ||
                    context.User.HasClaim("scp", "Warehouses.Manage") ||
                    context.User.Identity?.IsAuthenticated == true));
                    
            options.AddPolicy("ReportsRead", policy => 
                policy.RequireAssertion(context =>
                    context.User.HasClaim("scope", "Reports.Read") ||
                    context.User.HasClaim("scp", "Reports.Read") ||
                    context.User.Identity?.IsAuthenticated == true));
        });
        
        return services;
    }
    
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:PermitLimit", 100);
        var windowSeconds = configuration.GetValue("RateLimiting:WindowSeconds", 60);
        
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userId = context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                
                return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 2
                });
            });
        });
        
        return services;
    }
    
    public static IServiceCollection AddOpenTelemetryObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "inventory-api";
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();
                    
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                    
                if (!string.IsNullOrEmpty(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
                }
            });
            
        return services;
    }
}
