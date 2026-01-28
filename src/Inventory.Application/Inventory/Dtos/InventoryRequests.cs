namespace Inventory.Application.Inventory.Dtos;

public record ReceiptRequest(
    Guid ItemId,
    Guid WarehouseId,
    Guid? LocationId,
    decimal Quantity,
    decimal? UnitCost,
    string? ReferenceType,
    string? ReferenceId,
    string? LotNumber,
    DateOnly? ExpirationDate,
    DateTimeOffset? TransactionDate
);

public record IssueRequest(
    Guid ItemId,
    Guid WarehouseId,
    Guid? LocationId,
    decimal Quantity,
    string? ReferenceType,
    string? ReferenceId,
    DateTimeOffset? TransactionDate
);

public record AdjustmentRequest(
    Guid ItemId,
    Guid WarehouseId,
    Guid? LocationId,
    string AdjustmentType, // "increase" or "decrease"
    decimal Quantity,
    string ReasonCode,
    string? ReasonDescription,
    DateTimeOffset? TransactionDate
);

public record InventoryOperationResult(
    Guid TransactionId,
    BalanceSnapshotDto Balance
);

public record BalanceSnapshotDto(
    decimal OnHand,
    decimal Reserved,
    decimal Available,
    decimal InTransit
);
