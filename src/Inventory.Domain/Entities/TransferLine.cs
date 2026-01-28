using Inventory.Domain.Common;

namespace Inventory.Domain.Entities;

public class TransferLine : BaseEntity
{
    public Guid TransferId { get; set; }
    public Guid ItemId { get; set; }
    
    public decimal RequestedQuantity { get; set; }
    public decimal? ShippedQuantity { get; set; }
    public decimal? ReceivedQuantity { get; set; }
    
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? Notes { get; set; }
    
    // Navigation
    public virtual Transfer Transfer { get; set; } = null!;
    public virtual Item Item { get; set; } = null!;
}
