using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName)));
                
        services.AddScoped<IInventoryDbContext>(sp => sp.GetRequiredService<InventoryDbContext>());
        
        // Services
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<IIdempotencyService>(sp =>
        {
            var context = sp.GetRequiredService<IInventoryDbContext>();
            var tenantService = sp.GetRequiredService<ICurrentTenantService>();
            var dateTimeService = sp.GetRequiredService<IDateTimeService>();
            var ttlHours = configuration.GetValue<int>("Inventory:IdempotencyKeyTtlHours", 24);
            return new IdempotencyService(context, tenantService, dateTimeService, ttlHours);
        });
        
        return services;
    }
}
