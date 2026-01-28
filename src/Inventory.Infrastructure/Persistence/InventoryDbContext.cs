using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Common;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContext : DbContext, IInventoryDbContext
{
    private readonly ICurrentTenantService _tenantService;
    private readonly ICurrentUserService _userService;
    private readonly IDateTimeService _dateTimeService;
    
    public InventoryDbContext(
        DbContextOptions<InventoryDbContext> options,
        ICurrentTenantService tenantService,
        ICurrentUserService userService,
        IDateTimeService dateTimeService) : base(options)
    {
        _tenantService = tenantService;
        _userService = userService;
        _dateTimeService = dateTimeService;
    }
    
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<UnitOfMeasure> UnitsOfMeasure => Set<UnitOfMeasure>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferLine> TransferLines => Set<TransferLine>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Apply all configurations from assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Set default schema
        builder.HasDefaultSchema("inventory");
        
        // Apply global tenant filter to all tenant entities
        var tenantId = _tenantService.TenantId;
        
        builder.Entity<Item>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<Category>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<UnitOfMeasure>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<Warehouse>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<Location>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<InventoryTransaction>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<InventoryBalance>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<Transfer>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<Reservation>().HasQueryFilter(e => e.TenantId == tenantId);
        builder.Entity<IdempotencyKey>().HasQueryFilter(e => e.TenantId == tenantId);
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.TenantId = _tenantService.TenantId;
                    entry.Entity.CreatedAt = _dateTimeService.UtcNow;
                    entry.Entity.CreatedBy = _userService.UserName ?? "system";
                    break;
                    
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = _dateTimeService.UtcNow;
                    entry.Entity.UpdatedBy = _userService.UserName;
                    break;
            }
        }
        
        return await base.SaveChangesAsync(cancellationToken);
    }
}
