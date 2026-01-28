using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Endpoints;

public static class WarehousesEndpoints
{
    public static RouteGroupBuilder MapWarehousesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/warehouses")
            .WithTags("Warehouses")
            .RequireAuthorization("WarehousesManage");
            
        group.MapGet("/", GetWarehouses)
            .WithName("GetWarehouses")
            .WithSummary("Get paginated list of warehouses")
            .RequireAuthorization("InventoryRead");
            
        group.MapGet("/{id:guid}", GetWarehouseById)
            .WithName("GetWarehouseById")
            .WithSummary("Get warehouse by ID")
            .RequireAuthorization("InventoryRead");
            
        return group;
    }
    
    private static async Task<IResult> GetWarehouses(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        Application.Common.Interfaces.IInventoryDbContext context,
        CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        
        var query = context.Warehouses
            .AsNoTracking()
            .AsQueryable();
            
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(w => w.Code.Contains(search) || w.Name.Contains(search));
            
        if (isActive.HasValue)
            query = query.Where(w => w.IsActive == isActive.Value);
            
        var totalCount = await query.CountAsync(ct);
        
        var warehouses = await query
            .OrderBy(w => w.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new
            {
                w.Id,
                w.Code,
                w.Name,
                w.Address,
                w.IsActive
            })
            .ToListAsync(ct);
            
        return Results.Ok(new
        {
            data = warehouses,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            }
        });
    }
    
    private static async Task<IResult> GetWarehouseById(
        Guid id,
        Application.Common.Interfaces.IInventoryDbContext context,
        CancellationToken ct)
    {
        var warehouse = await context.Warehouses
            .AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new
            {
                w.Id,
                w.Code,
                w.Name,
                w.Address,
                w.IsActive,
                w.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
            
        return warehouse is not null
            ? Results.Ok(warehouse)
            : Results.NotFound(new { errorCode = "WAREHOUSE_NOT_FOUND", message = "Warehouse not found" });
    }
}
