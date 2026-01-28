using Inventory.Application.Transfers;
using Inventory.Application.Transfers.Dtos;
using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Endpoints;

public static class TransfersEndpoints
{
    public static RouteGroupBuilder MapTransfersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/transfers")
            .WithTags("Transfers")
            .RequireAuthorization("InventoryWrite");
            
        group.MapGet("/", GetTransfers)
            .WithName("GetTransfers")
            .WithSummary("Get paginated list of transfers")
            .RequireAuthorization("InventoryRead");
            
        group.MapGet("/{id:guid}", GetTransferById)
            .WithName("GetTransferById")
            .WithSummary("Get transfer by ID")
            .RequireAuthorization("InventoryRead");
            
        group.MapPost("/", CreateTransfer)
            .WithName("CreateTransfer")
            .WithSummary("Create a new transfer");
            
        group.MapPost("/{id:guid}/commit", CommitTransfer)
            .WithName("CommitTransfer")
            .WithSummary("Commit a draft transfer");
            
        group.MapPost("/{id:guid}/ship", ShipTransfer)
            .WithName("ShipTransfer")
            .WithSummary("Ship a committed transfer");
            
        group.MapPost("/{id:guid}/receive", ReceiveTransfer)
            .WithName("ReceiveTransfer")
            .WithSummary("Receive a shipped transfer");
            
        group.MapPost("/{id:guid}/cancel", CancelTransfer)
            .WithName("CancelTransfer")
            .WithSummary("Cancel a transfer");
            
        return group;
    }
    
    private static async Task<IResult> GetTransfers(
        [FromQuery] TransferStatus? status,
        [FromQuery] Guid? sourceWarehouseId,
        [FromQuery] Guid? destinationWarehouseId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ITransferService transferService,
        CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        
        var result = await transferService.GetTransfersAsync(
            status, sourceWarehouseId, destinationWarehouseId, page, pageSize, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> GetTransferById(
        Guid id,
        ITransferService transferService,
        CancellationToken ct)
    {
        var result = await transferService.GetTransferByIdAsync(id, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> CreateTransfer(
        [FromBody] CreateTransferRequest request,
        ITransferService transferService,
        CancellationToken ct)
    {
        var result = await transferService.CreateTransferAsync(request, ct);
        return result.IsSuccess
            ? Results.Created($"/v1/transfers/{result.Value!.Id}", result.Value)
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> CommitTransfer(
        Guid id,
        ITransferService transferService,
        CancellationToken ct)
    {
        var result = await transferService.CommitTransferAsync(id, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> ShipTransfer(
        Guid id,
        [FromBody] ShipTransferRequest request,
        ITransferService transferService,
        CancellationToken ct)
    {
        var result = await transferService.ShipTransferAsync(id, request, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> ReceiveTransfer(
        Guid id,
        [FromBody] ReceiveTransferRequest request,
        ITransferService transferService,
        CancellationToken ct)
    {
        var result = await transferService.ReceiveTransferAsync(id, request, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> CancelTransfer(
        Guid id,
        ITransferService transferService,
        CancellationToken ct)
    {
        var result = await transferService.CancelTransferAsync(id, ct);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
}
