namespace Inventory.Domain.Exceptions;

public class InsufficientStockException : DomainException
{
    public Guid ItemId { get; }
    public decimal RequestedQuantity { get; }
    public decimal AvailableQuantity { get; }
    
    public InsufficientStockException(Guid itemId, decimal requested, decimal available)
        : base("INSUFFICIENT_STOCK", 
            $"Cannot process {requested} units. Available stock: {available}")
    {
        ItemId = itemId;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}
