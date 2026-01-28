namespace Inventory.Domain.Common;

public abstract class AuditableEntity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
