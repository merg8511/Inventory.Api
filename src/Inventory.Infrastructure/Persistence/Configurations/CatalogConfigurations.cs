using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("warehouses");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(20).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Address).HasColumnName("address").HasMaxLength(500);
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
        
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
    }
}

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.WarehouseId).HasColumnName("warehouse_id").IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100);
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        
        builder.HasIndex(e => new { e.WarehouseId, e.Code }).IsUnique();
        
        builder.HasOne(e => e.Warehouse)
            .WithMany(w => w.Locations)
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.ParentCategoryId).HasColumnName("parent_category_id");
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
        
        builder.HasOne(e => e.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(e => e.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("units_of_measure");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(10).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(100);
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100);
        
        builder.HasIndex(e => new { e.TenantId, e.Code }).IsUnique();
    }
}
