using Inventory.Domain.Common;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

/// <summary>
/// Ledger/Kardex entry - source of truth for all inventory movements
/// </summary>
public class InventoryTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? LocationId { get; set; }
    
    public TransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReasonCode { get; set; }
    public string? ReasonDescription { get; set; }
    
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    
    public DateTimeOffset TransactionDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? IdempotencyKey { get; set; }
    
    // Navigation
    public virtual Item Item { get; set; } = null!;
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual Location? Location { get; set; }
}
