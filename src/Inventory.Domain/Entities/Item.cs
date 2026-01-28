using Inventory.Domain.Common;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public class Item : AuditableEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid UnitOfMeasureId { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public TrackingType TrackingType { get; set; } = TrackingType.None;
    public decimal? MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
    public int RowVersion { get; set; } = 1;
    
    // Navigation
    public virtual Category? Category { get; set; }
    public virtual UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    public virtual ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
