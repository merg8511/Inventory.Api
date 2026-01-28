using Inventory.Domain.Common;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Entities;

public class Reservation : AuditableEntity
{
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? LocationId { get; set; }
    
    public decimal Quantity { get; set; }
    
    public string OrderType { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTimeOffset? ExpiresAt { get; set; }
    
    public string? CorrelationId { get; set; }
    
    // Navigation
    public virtual Item Item { get; set; } = null!;
    public virtual Warehouse Warehouse { get; set; } = null!;
    public virtual Location? Location { get; set; }
    
    // State methods
    public void Confirm()
    {
        if (Status != ReservationStatus.Active)
            throw new DomainException("INVALID_STATUS", 
                $"Cannot confirm reservation in status {Status}. Must be Active.");
                
        Status = ReservationStatus.Confirmed;
    }
    
    public void Release()
    {
        if (Status != ReservationStatus.Active)
            throw new DomainException("INVALID_STATUS", 
                $"Cannot release reservation in status {Status}. Must be Active.");
                
        Status = ReservationStatus.Released;
    }
    
    public void Cancel()
    {
        if (Status != ReservationStatus.Active)
            throw new DomainException("INVALID_STATUS", 
                $"Cannot cancel reservation in status {Status}. Must be Active.");
                
        Status = ReservationStatus.Cancelled;
    }
    
    public bool IsExpired(DateTimeOffset now) => 
        ExpiresAt.HasValue && ExpiresAt.Value < now && Status == ReservationStatus.Active;
}
