namespace Inventory.Application.Inventory.Dtos;

public record BalanceDto(
    Guid Id,
    Guid ItemId,
    string ItemSku,
    string ItemName,
    Guid WarehouseId,
    string WarehouseCode,
    Guid? LocationId,
    string? LocationCode,
    decimal OnHand,
    decimal Reserved,
    decimal Available,
    decimal InTransit,
    DateTimeOffset? LastTransactionDate
);

public record TransactionDto(
    Guid Id,
    Guid ItemId,
    string ItemSku,
    Guid WarehouseId,
    string WarehouseCode,
    string TransactionType,
    decimal Quantity,
    decimal? UnitCost,
    decimal? TotalCost,
    string? ReferenceType,
    string? ReferenceId,
    string? ReasonCode,
    string? ReasonDescription,
    DateTimeOffset TransactionDate,
    string CreatedBy
);
