using Inventory.Application.Inventory;
using Inventory.Application.Inventory.Dtos;
using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Endpoints;

public static class InventoryEndpoints
{
    public static RouteGroupBuilder MapInventoryEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/inventory")
            .WithTags("Inventory");
            
        group.MapGet("/balances", GetBalances)
            .WithName("GetBalances")
            .WithSummary("Get inventory balances")
            .RequireAuthorization("InventoryRead");
            
        group.MapGet("/transactions", GetTransactions)
            .WithName("GetTransactions")
            .WithSummary("Get inventory transaction history")
            .RequireAuthorization("InventoryRead");
            
        group.MapPost("/receipt", Receipt)
            .WithName("ReceiptInventory")
            .WithSummary("Record inventory receipt")
            .RequireAuthorization("InventoryWrite");
            
        group.MapPost("/issue", Issue)
            .WithName("IssueInventory")
            .WithSummary("Record inventory issue")
            .RequireAuthorization("InventoryWrite");
            
        group.MapPost("/adjustment", Adjustment)
            .WithName("AdjustInventory")
            .WithSummary("Record inventory adjustment")
            .RequireAuthorization("InventoryWrite");
            
        return group;
    }
    
    private static async Task<IResult> GetBalances(
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IInventoryService inventoryService,
        CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        
        var result = await inventoryService.GetBalancesAsync(itemId, warehouseId, page, pageSize, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> GetTransactions(
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] TransactionType? transactionType,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IInventoryService inventoryService,
        CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        
        var result = await inventoryService.GetTransactionsAsync(
            itemId, warehouseId, fromDate, toDate, transactionType, page, pageSize, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> Receipt(
        [FromBody] ReceiptRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        IInventoryService inventoryService,
        CancellationToken ct)
    {
        var result = await inventoryService.ReceiptAsync(request, idempotencyKey, ct);
        return result.IsSuccess
            ? Results.Created($"/v1/inventory/transactions/{result.Value!.TransactionId}", result.Value)
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> Issue(
        [FromBody] IssueRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        IInventoryService inventoryService,
        CancellationToken ct)
    {
        var result = await inventoryService.IssueAsync(request, idempotencyKey, ct);
        return result.IsSuccess
            ? Results.Created($"/v1/inventory/transactions/{result.Value!.TransactionId}", result.Value)
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> Adjustment(
        [FromBody] AdjustmentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        IInventoryService inventoryService,
        CancellationToken ct)
    {
        var result = await inventoryService.AdjustmentAsync(request, idempotencyKey, ct);
        return result.IsSuccess
            ? Results.Created($"/v1/inventory/transactions/{result.Value!.TransactionId}", result.Value)
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
}
