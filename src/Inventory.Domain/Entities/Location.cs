using Inventory.Domain.Common;

namespace Inventory.Domain.Entities;

public class Location : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
