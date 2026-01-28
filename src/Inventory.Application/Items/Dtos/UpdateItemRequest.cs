namespace Inventory.Application.Items.Dtos;

public record UpdateItemRequest(
    string Name,
    string? Description,
    Guid? CategoryId,
    Guid UnitOfMeasureId,
    decimal CostPrice,
    decimal SalePrice,
    decimal? MinimumStock,
    int RowVersion
);
