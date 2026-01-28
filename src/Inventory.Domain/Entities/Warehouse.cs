using Inventory.Domain.Common;

namespace Inventory.Domain.Entities;

public class Warehouse : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
    public virtual ICollection<InventoryBalance> InventoryBalances { get; set; } = new List<InventoryBalance>();
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public virtual ICollection<Transfer> OutgoingTransfers { get; set; } = new List<Transfer>();
    public virtual ICollection<Transfer> IncomingTransfers { get; set; } = new List<Transfer>();
}
