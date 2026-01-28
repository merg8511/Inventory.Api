using Inventory.Domain.Common;

namespace Inventory.Domain.Entities;

public class UnitOfMeasure : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
