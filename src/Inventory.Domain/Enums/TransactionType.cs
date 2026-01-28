namespace Inventory.Domain.Enums;

public enum TransactionType
{
    Receipt = 1,
    Issue = 2,
    PositiveAdjustment = 3,
    NegativeAdjustment = 4,
    TransferOut = 5,
    TransferIn = 6,
    Reserve = 7,
    Unreserve = 8
}
