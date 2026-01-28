using Inventory.Domain.Common;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities;

/// <summary>
/// Snapshot/Projection of current stock levels - optimized for queries
/// </summary>
public class InventoryBalance : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? LocationId { get; set; }
    
    public decimal OnHand { get; set; }
    public decimal Reserved { get; set; }
    public decimal InTransit { get; set; }
    
    public Guid? LastTransactionId { get; set; }
    public DateTimeOffset? LastTransactionDate { get; set; }
    
    public int RowVersion { get; set; } = 1;
    public DateTimeOffset UpdatedAt { get; set; }
    
    // Computed property
    public decimal Available => OnHand - Reserved;
    
    // Navigation
    public virtual Item Item { get; set; } = null!;
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual Location? Location { get; set; }
    public virtual InventoryTransaction? LastTransaction { get; set; }
    
    // Domain methods
    public void AddStock(decimal quantity, Guid transactionId, DateTimeOffset transactionDate)
    {
        if (quantity <= 0)
            throw new DomainException("INVALID_QUANTITY", "Quantity must be positive");
            
        OnHand += quantity;
        LastTransactionId = transactionId;
        LastTransactionDate = transactionDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    
    public void RemoveStock(decimal quantity, Guid transactionId, DateTimeOffset transactionDate, bool allowNegative = false)
    {
        if (quantity <= 0)
            throw new DomainException("INVALID_QUANTITY", "Quantity must be positive");
            
        if (!allowNegative && Available < quantity)
            throw new InsufficientStockException(ItemId, quantity, Available);
            
        OnHand -= quantity;
        LastTransactionId = transactionId;
        LastTransactionDate = transactionDate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    
    public void Reserve(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("INVALID_QUANTITY", "Quantity must be positive");
            
        if (Available < quantity)
            throw new InsufficientStockException(ItemId, quantity, Available);
            
        Reserved += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    
    public void Unreserve(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("INVALID_QUANTITY", "Quantity must be positive");
            
        if (Reserved < quantity)
            throw new DomainException("INVALID_UNRESERVE", "Cannot unreserve more than reserved");
            
        Reserved -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    
    public void AddInTransit(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("INVALID_QUANTITY", "Quantity must be positive");
            
        InTransit += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    
    public void RemoveInTransit(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("INVALID_QUANTITY", "Quantity must be positive");
            
        InTransit -= quantity;
        if (InTransit < 0) InTransit = 0;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
