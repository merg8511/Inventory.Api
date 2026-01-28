using Inventory.Application.Items;
using Inventory.Application.Items.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Endpoints;

public static class ItemsEndpoints
{
    public static RouteGroupBuilder MapItemsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/items")
            .WithTags("Items")
            .RequireAuthorization("ItemsManage");
            
        group.MapGet("/", GetItems)
            .WithName("GetItems")
            .WithSummary("Get paginated list of items")
            .RequireAuthorization("InventoryRead");
            
        group.MapGet("/{id:guid}", GetItemById)
            .WithName("GetItemById")
            .WithSummary("Get item by ID")
            .RequireAuthorization("InventoryRead");
            
        group.MapPost("/", CreateItem)
            .WithName("CreateItem")
            .WithSummary("Create a new item");
            
        group.MapPut("/{id:guid}", UpdateItem)
            .WithName("UpdateItem")
            .WithSummary("Update an existing item");
            
        group.MapDelete("/{id:guid}", DeactivateItem)
            .WithName("DeactivateItem")
            .WithSummary("Deactivate an item (soft delete)");
            
        return group;
    }
    
    private static async Task<IResult> GetItems(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? isActive,
        IItemsService itemsService,
        CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        
        var result = await itemsService.GetItemsAsync(page, pageSize, search, categoryId, isActive, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> GetItemById(
        Guid id,
        IItemsService itemsService,
        CancellationToken ct)
    {
        var result = await itemsService.GetItemByIdAsync(id, ct);
        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : Results.NotFound(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> CreateItem(
        [FromBody] CreateItemRequest request,
        IItemsService itemsService,
        CancellationToken ct)
    {
        var result = await itemsService.CreateItemAsync(request, ct);
        return result.IsSuccess
            ? Results.Created($"/v1/items/{result.Value!.Id}", result.Value)
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> UpdateItem(
        Guid id,
        [FromBody] UpdateItemRequest request,
        IItemsService itemsService,
        CancellationToken ct)
    {
        var result = await itemsService.UpdateItemAsync(id, request, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> DeactivateItem(
        Guid id,
        IItemsService itemsService,
        CancellationToken ct)
    {
        var result = await itemsService.DeactivateItemAsync(id, ct);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.NotFound(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
}
