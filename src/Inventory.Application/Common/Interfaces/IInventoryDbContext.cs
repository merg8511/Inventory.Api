using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Common.Interfaces;

public interface IInventoryDbContext
{
    DbSet<Item> Items { get; }
    DbSet<Category> Categories { get; }
    DbSet<UnitOfMeasure> UnitsOfMeasure { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<Location> Locations { get; }
    DbSet<InventoryTransaction> InventoryTransactions { get; }
    DbSet<InventoryBalance> InventoryBalances { get; }
    DbSet<Transfer> Transfers { get; }
    DbSet<TransferLine> TransferLines { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<IdempotencyKey> IdempotencyKeys { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
