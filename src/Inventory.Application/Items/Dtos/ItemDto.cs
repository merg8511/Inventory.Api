namespace Inventory.Application.Items.Dtos;

public record ItemDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    Guid? CategoryId,
    string? CategoryName,
    Guid UnitOfMeasureId,
    string UnitOfMeasureCode,
    decimal CostPrice,
    decimal SalePrice,
    string TrackingType,
    decimal? MinimumStock,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public record ItemListDto(
    Guid Id,
    string Sku,
    string Name,
    string? CategoryName,
    string UnitOfMeasureCode,
    decimal SalePrice,
    bool IsActive
);
