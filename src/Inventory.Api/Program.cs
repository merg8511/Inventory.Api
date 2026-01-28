using Inventory.Api.Endpoints;
using Inventory.Api.Extensions;
using Inventory.Api.Middleware;
using Inventory.Application;
using Inventory.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Inventory API");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Serilog
    builder.Host.UseSerilog((context, loggerConfig) =>
        loggerConfig.ReadFrom.Configuration(context.Configuration));
    
    // Add services
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerWithAuth();
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorizationWithPolicies();
    builder.Services.AddRateLimiting(builder.Configuration);
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);
    builder.Services.AddOpenTelemetryObservability(builder.Configuration);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddProblemDetails();
    
    // Application & Infrastructure
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    
    var app = builder.Build();
    
    // Middleware pipeline
    // IMPORTANT: Order matters! CorrelationId and ExceptionHandling run early
    app.UseSerilogRequestLogging();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1");
            c.RoutePrefix = "swagger";
        });
    }
    
    // CRITICAL: Authentication MUST run before TenantMiddleware
    // so that context.User contains the JWT claims including "tenant_id"
    app.UseAuthentication();
    app.UseMiddleware<TenantMiddleware>(); // Reads tenant_id from authenticated user claims
    app.UseAuthorization();
    app.UseRateLimiter();
    
    // Health endpoints
    app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
    app.MapHealthChecks("/health/ready");
    
    // API endpoints
    app.MapItemsEndpoints();
    app.MapWarehousesEndpoints();
    app.MapInventoryEndpoints();
    app.MapTransfersEndpoints();
    app.MapReservationsEndpoints();
    app.MapDevEndpoints(); // Development token generator
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// For integration testing
public partial class Program { }
