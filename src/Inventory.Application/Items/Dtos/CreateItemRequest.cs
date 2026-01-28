using Inventory.Domain.Enums;

namespace Inventory.Application.Items.Dtos;

public record CreateItemRequest(
    string Sku,
    string Name,
    string? Description,
    Guid? CategoryId,
    Guid UnitOfMeasureId,
    decimal CostPrice,
    decimal SalePrice,
    TrackingType TrackingType = TrackingType.None,
    decimal? MinimumStock = null
);
