using Inventory.Domain.Common;

namespace Inventory.Domain.Entities;

public class Category : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation
    public virtual Category? ParentCategory { get; set; }
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public virtual ICollection<Item> Items { get; set; } = new List<Item>();
}
