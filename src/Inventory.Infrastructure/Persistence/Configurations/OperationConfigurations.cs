using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.TransferNumber).HasColumnName("transfer_number").HasMaxLength(30).IsRequired();
        builder.Property(e => e.SourceWarehouseId).HasColumnName("source_warehouse_id").IsRequired();
        builder.Property(e => e.SourceLocationId).HasColumnName("source_location_id");
        builder.Property(e => e.DestinationWarehouseId).HasColumnName("destination_warehouse_id").IsRequired();
        builder.Property(e => e.DestinationLocationId).HasColumnName("destination_location_id");
        builder.Property(e => e.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(e => e.CommittedAt).HasColumnName("committed_at");
        builder.Property(e => e.ShippedAt).HasColumnName("shipped_at");
        builder.Property(e => e.ReceivedAt).HasColumnName("received_at");
        builder.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
        builder.Property(e => e.RowVersion).HasColumnName("row_version").IsConcurrencyToken();
        
        builder.HasIndex(e => new { e.TenantId, e.TransferNumber }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => e.SourceWarehouseId);
        builder.HasIndex(e => e.DestinationWarehouseId);
        
        builder.HasOne(e => e.SourceWarehouse)
            .WithMany(w => w.OutgoingTransfers)
            .HasForeignKey(e => e.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(e => e.DestinationWarehouse)
            .WithMany(w => w.IncomingTransfers)
            .HasForeignKey(e => e.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TransferLineConfiguration : IEntityTypeConfiguration<TransferLine>
{
    public void Configure(EntityTypeBuilder<TransferLine> builder)
    {
        builder.ToTable("transfer_lines");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TransferId).HasColumnName("transfer_id").IsRequired();
        builder.Property(e => e.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(e => e.RequestedQuantity).HasColumnName("requested_quantity").HasPrecision(18, 4);
        builder.Property(e => e.ShippedQuantity).HasColumnName("shipped_quantity").HasPrecision(18, 4);
        builder.Property(e => e.ReceivedQuantity).HasColumnName("received_quantity").HasPrecision(18, 4);
        builder.Property(e => e.LotNumber).HasColumnName("lot_number").HasMaxLength(50);
        builder.Property(e => e.SerialNumber).HasColumnName("serial_number").HasMaxLength(100);
        builder.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(500);
        
        builder.HasIndex(e => e.TransferId);
        builder.HasIndex(e => e.ItemId);
        
        builder.HasOne(e => e.Transfer)
            .WithMany(t => t.Lines)
            .HasForeignKey(e => e.TransferId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.Item)
            .WithMany()
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.ItemId).HasColumnName("item_id").IsRequired();
        builder.Property(e => e.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(e => e.LocationId).HasColumnName("location_id");
        builder.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 4);
        builder.Property(e => e.OrderType).HasColumnName("order_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.OrderId).HasColumnName("order_id").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        
        builder.HasIndex(e => new { e.TenantId, e.ItemId });
        builder.HasIndex(e => new { e.TenantId, e.OrderType, e.OrderId });
        builder.HasIndex(e => e.ExpiresAt);
        
        builder.HasOne(e => e.Item)
            .WithMany()
            .HasForeignKey(e => e.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
        builder.Property(e => e.RequestHash).HasColumnName("request_hash").HasMaxLength(64).IsRequired();
        builder.Property(e => e.ResponseStatusCode).HasColumnName("response_status_code").IsRequired();
        builder.Property(e => e.ResponseBody).HasColumnName("response_body").IsRequired();
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");
        
        builder.HasIndex(e => new { e.TenantId, e.Key }).IsUnique();
        builder.HasIndex(e => e.ExpiresAt);
    }
}
