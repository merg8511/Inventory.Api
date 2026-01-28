using Inventory.Domain.Common;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities;

public class Transfer : AuditableEntity
{
    public string TransferNumber { get; set; } = string.Empty;
    
    public Guid SourceWarehouseId { get; set; }
    public Guid? SourceLocationId { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public Guid? DestinationLocationId { get; set; }
    
    public TransferStatus Status { get; set; } = TransferStatus.Draft;
    public string? Notes { get; set; }
    
    public DateTimeOffset? CommittedAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    
    public int RowVersion { get; set; } = 1;
    
    // Navigation
    public virtual Warehouse SourceWarehouse { get; set; } = null!;
    public virtual Location? SourceLocation { get; set; }
    public virtual Warehouse DestinationWarehouse { get; set; } = null!;
    public virtual Location? DestinationLocation { get; set; }
    public virtual ICollection<TransferLine> Lines { get; set; } = new List<TransferLine>();
    
    // State machine methods
    public void Commit(DateTimeOffset timestamp)
    {
        if (Status != TransferStatus.Draft)
            throw new DomainException("INVALID_STATUS", 
                $"Cannot commit transfer in status {Status}. Must be Draft.");
                
        if (!Lines.Any())
            throw new DomainException("EMPTY_TRANSFER", "Transfer must have at least one line.");
            
        Status = TransferStatus.Committed;
        CommittedAt = timestamp;
    }
    
    public void Ship(DateTimeOffset timestamp)
    {
        if (Status != TransferStatus.Committed)
            throw new DomainException("INVALID_STATUS", 
                $"Cannot ship transfer in status {Status}. Must be Committed.");
                
        Status = TransferStatus.InTransit;
        ShippedAt = timestamp;
    }
    
    public void Receive(DateTimeOffset timestamp)
    {
        if (Status != TransferStatus.InTransit)
            throw new DomainException("INVALID_STATUS", 
                $"Cannot receive transfer in status {Status}. Must be InTransit.");
                
        Status = TransferStatus.Received;
        ReceivedAt = timestamp;
    }
    
    public void Cancel(DateTimeOffset timestamp)
    {
        if (Status == TransferStatus.Received || Status == TransferStatus.Cancelled)
            throw new DomainException("INVALID_STATUS", 
                $"Cannot cancel transfer in status {Status}.");
                
        Status = TransferStatus.Cancelled;
        CancelledAt = timestamp;
    }
}
