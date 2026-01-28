namespace Inventory.Domain.Enums;

public enum TransferStatus
{
    Draft = 1,
    Committed = 2,
    InTransit = 3,
    Received = 4,
    Cancelled = 5
}
