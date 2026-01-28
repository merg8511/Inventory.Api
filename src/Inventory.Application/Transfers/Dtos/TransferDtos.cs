using Inventory.Domain.Enums;

namespace Inventory.Application.Transfers.Dtos;

public record TransferDto(
    Guid Id,
    string TransferNumber,
    Guid SourceWarehouseId,
    string SourceWarehouseCode,
    Guid DestinationWarehouseId,
    string DestinationWarehouseCode,
    string Status,
    string? Notes,
    DateTimeOffset? CommittedAt,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? ReceivedAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TransferLineDto> Lines
);

public record TransferLineDto(
    Guid Id,
    Guid ItemId,
    string ItemSku,
    string ItemName,
    decimal RequestedQuantity,
    decimal? ShippedQuantity,
    decimal? ReceivedQuantity,
    string? LotNumber
);

public record CreateTransferRequest(
    Guid SourceWarehouseId,
    Guid? SourceLocationId,
    Guid DestinationWarehouseId,
    Guid? DestinationLocationId,
    string? Notes,
    IReadOnlyList<CreateTransferLineRequest> Lines
);

public record CreateTransferLineRequest(
    Guid ItemId,
    decimal RequestedQuantity,
    string? LotNumber = null,
    string? SerialNumber = null
);

public record ShipTransferRequest(
    IReadOnlyList<ShipLineRequest> Lines
);

public record ShipLineRequest(
    Guid LineId,
    decimal ShippedQuantity
);

public record ReceiveTransferRequest(
    IReadOnlyList<ReceiveLineRequest> Lines
);

public record ReceiveLineRequest(
    Guid LineId,
    decimal ReceivedQuantity
);
