using FluentValidation;
using Inventory.Application.Inventory;
using Inventory.Application.Items;
using Inventory.Application.Reservations;
using Inventory.Application.Transfers;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register validators
        services.AddValidatorsFromAssemblyContaining<IItemsService>();
        
        // Register services
        services.AddScoped<IItemsService, ItemsService>();
        services.AddScoped<IInventoryService>(sp =>
        {
            var context = sp.GetRequiredService<Common.Interfaces.IInventoryDbContext>();
            var tenantService = sp.GetRequiredService<Common.Interfaces.ICurrentTenantService>();
            var userService = sp.GetRequiredService<Common.Interfaces.ICurrentUserService>();
            var dateTimeService = sp.GetRequiredService<Common.Interfaces.IDateTimeService>();
            // TODO: Get from configuration
            var allowNegativeStock = false;
            return new InventoryService(context, tenantService, userService, dateTimeService, allowNegativeStock);
        });
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<IReservationService, ReservationService>();
        
        return services;
    }
}
