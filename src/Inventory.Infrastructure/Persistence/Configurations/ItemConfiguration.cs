using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.ToTable("items");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
        builder.Property(e => e.CategoryId).HasColumnName("category_id");
        builder.Property(e => e.UnitOfMeasureId).HasColumnName("unit_of_measure_id").IsRequired();
        builder.Property(e => e.CostPrice).HasColumnName("cost_price").HasPrecision(18, 4);
        builder.Property(e => e.SalePrice).HasColumnName("sale_price").HasPrecision(18, 4);
        builder.Property(e => e.TrackingType).HasColumnName("tracking_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.MinimumStock).HasColumnName("minimum_stock").HasPrecision(18, 4);
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
        builder.Property(e => e.RowVersion).HasColumnName("row_version")
            .IsConcurrencyToken();
        
        builder.HasIndex(e => new { e.TenantId, e.Sku }).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.CategoryId });
        
        builder.HasOne(e => e.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(e => e.UnitOfMeasure)
            .WithMany(u => u.Items)
            .HasForeignKey(e => e.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
