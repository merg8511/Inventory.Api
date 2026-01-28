using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("inventory_transactions");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(e => e.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(e => e.LocationId).HasColumnName("location_id");
        builder.Property(e => e.TransactionType).HasColumnName("transaction_type")
            .HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 4).IsRequired();
        builder.Property(e => e.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 4);
        builder.Property(e => e.TotalCost).HasColumnName("total_cost").HasPrecision(18, 4);
        builder.Property(e => e.ReferenceType).HasColumnName("reference_type").HasMaxLength(50);
        builder.Property(e => e.ReferenceId).HasColumnName("reference_id").HasMaxLength(100);
        builder.Property(e => e.ReasonCode).HasColumnName("reason_code").HasMaxLength(30);
        builder.Property(e => e.ReasonDescription).HasColumnName("reason_description").HasMaxLength(500);
        builder.Property(e => e.LotNumber).HasColumnName("lot_number").HasMaxLength(50);
        builder.Property(e => e.SerialNumber).HasColumnName("serial_number").HasMaxLength(100);
        builder.Property(e => e.ExpirationDate).HasColumnName("expiration_date");
        builder.Property(e => e.TransactionDate).HasColumnName("transaction_date").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100);
        
        // Indexes
        builder.HasIndex(e => new { e.TenantId, e.ItemId });
        builder.HasIndex(e => new { e.TenantId, e.WarehouseId });
        builder.HasIndex(e => new { e.TenantId, e.TransactionDate });
        builder.HasIndex(e => new { e.ReferenceType, e.ReferenceId });
        builder.HasIndex(e => e.CorrelationId);
        
        // Relationships
        builder.HasOne(e => e.Item)
            .WithMany(i => i.InventoryTransactions)
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(e => e.Warehouse)
            .WithMany(w => w.InventoryTransactions)
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(e => e.Location)
            .WithMany(l => l.InventoryTransactions)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.ToTable("inventory_balances");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(e => e.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(e => e.LocationId).HasColumnName("location_id");
        builder.Property(e => e.OnHand).HasColumnName("on_hand").HasPrecision(18, 4);
        builder.Property(e => e.Reserved).HasColumnName("reserved").HasPrecision(18, 4);
        builder.Property(e => e.InTransit).HasColumnName("in_transit").HasPrecision(18, 4);
        builder.Property(e => e.LastTransactionId).HasColumnName("last_transaction_id");
        builder.Property(e => e.LastTransactionDate).HasColumnName("last_transaction_date");
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsConcurrencyToken();
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        
        // Computed column not directly supported - Available calculated in app layer
        builder.Ignore(e => e.Available);
        
        // Unique constraint
        builder.HasIndex(e => new { e.TenantId, e.ItemId, e.WarehouseId, e.LocationId }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.ItemId });
        builder.HasIndex(e => new { e.TenantId, e.WarehouseId });
        
        // Relationships
        builder.HasOne(e => e.Item)
            .WithMany(i => i.InventoryBalances)
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(e => e.Warehouse)
            .WithMany(w => w.InventoryBalances)
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(e => e.Location)
            .WithMany(l => l.InventoryBalances)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(e => e.LastTransaction)
            .WithMany()
            .HasForeignKey(e => e.LastTransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
