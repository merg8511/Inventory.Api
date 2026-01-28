namespace Inventory.Application.Reservations.Dtos;

public record ReservationDto(
    Guid Id,
    Guid ItemId,
    string ItemSku,
    string ItemName,
    Guid WarehouseId,
    string WarehouseCode,
    decimal Quantity,
    string OrderType,
    string OrderId,
    string Status,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt
);

public record CreateReservationRequest(
    Guid ItemId,
    Guid WarehouseId,
    Guid? LocationId,
    decimal Quantity,
    string OrderType,
    string OrderId,
    DateTimeOffset? ExpiresAt
);
