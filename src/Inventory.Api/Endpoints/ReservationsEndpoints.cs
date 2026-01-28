using Inventory.Application.Reservations;
using Inventory.Application.Reservations.Dtos;
using Inventory.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Endpoints;

public static class ReservationsEndpoints
{
    public static RouteGroupBuilder MapReservationsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/reservations")
            .WithTags("Reservations")
            .RequireAuthorization("InventoryWrite");
            
        group.MapGet("/", GetReservations)
            .WithName("GetReservations")
            .WithSummary("Get paginated list of reservations")
            .RequireAuthorization("InventoryRead");
            
        group.MapGet("/{id:guid}", GetReservationById)
            .WithName("GetReservationById")
            .WithSummary("Get reservation by ID")
            .RequireAuthorization("InventoryRead");
            
        group.MapPost("/", CreateReservation)
            .WithName("CreateReservation")
            .WithSummary("Create a new reservation");
            
        group.MapPost("/{id:guid}/confirm", ConfirmReservation)
            .WithName("ConfirmReservation")
            .WithSummary("Confirm reservation and issue stock");
            
        group.MapPost("/{id:guid}/release", ReleaseReservation)
            .WithName("ReleaseReservation")
            .WithSummary("Release a reservation");
            
        return group;
    }
    
    private static async Task<IResult> GetReservations(
        [FromQuery] Guid? itemId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] ReservationStatus? status,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        IReservationService reservationService,
        CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize > 100 ? 100 : pageSize;
        
        var result = await reservationService.GetReservationsAsync(
            itemId, warehouseId, status, page, pageSize, ct);
        return Results.Ok(result);
    }
    
    private static async Task<IResult> GetReservationById(
        Guid id,
        IReservationService reservationService,
        CancellationToken ct)
    {
        var result = await reservationService.GetReservationByIdAsync(id, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.NotFound(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> CreateReservation(
        [FromBody] CreateReservationRequest request,
        IReservationService reservationService,
        CancellationToken ct)
    {
        var result = await reservationService.CreateReservationAsync(request, ct);
        return result.IsSuccess
            ? Results.Created($"/v1/reservations/{result.Value!.Id}", result.Value)
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> ConfirmReservation(
        Guid id,
        IReservationService reservationService,
        CancellationToken ct)
    {
        var result = await reservationService.ConfirmReservationAsync(id, ct);
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
    
    private static async Task<IResult> ReleaseReservation(
        Guid id,
        IReservationService reservationService,
        CancellationToken ct)
    {
        var result = await reservationService.ReleaseReservationAsync(id, ct);
        return result.IsSuccess
            ? Results.NoContent()
            : Results.BadRequest(new { errorCode = result.ErrorCode, message = result.ErrorMessage });
    }
}
