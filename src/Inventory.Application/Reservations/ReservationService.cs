using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using Inventory.Application.Reservations.Dtos;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Reservations;

public interface IReservationService
{
    Task<PagedResult<ReservationDto>> GetReservationsAsync(Guid? itemId, Guid? warehouseId,
        ReservationStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<Result<ReservationDto>> GetReservationByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ReservationDto>> CreateReservationAsync(CreateReservationRequest request, CancellationToken ct = default);
    Task<Result<ReservationDto>> ConfirmReservationAsync(Guid id, CancellationToken ct = default);
    Task<Result> ReleaseReservationAsync(Guid id, CancellationToken ct = default);
}

public class ReservationService : IReservationService
{
    private readonly IInventoryDbContext _context;
    private readonly ICurrentTenantService _tenantService;
    private readonly ICurrentUserService _userService;
    private readonly IDateTimeService _dateTimeService;
    
    public ReservationService(
        IInventoryDbContext context,
        ICurrentTenantService tenantService,
        ICurrentUserService userService,
        IDateTimeService dateTimeService)
    {
        _context = context;
        _tenantService = tenantService;
        _userService = userService;
        _dateTimeService = dateTimeService;
    }
    
    public async Task<PagedResult<ReservationDto>> GetReservationsAsync(Guid? itemId, Guid? warehouseId,
        ReservationStatus? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Reservations
            .AsNoTracking()
            .Include(r => r.Item)
            .Include(r => r.Warehouse)
            .AsQueryable();
            
        if (itemId.HasValue)
            query = query.Where(r => r.ItemId == itemId);
        if (warehouseId.HasValue)
            query = query.Where(r => r.WarehouseId == warehouseId);
        if (status.HasValue)
            query = query.Where(r => r.Status == status);
            
        var totalCount = await query.CountAsync(ct);
        
        var reservations = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReservationDto(
                r.Id,
                r.ItemId,
                r.Item.Sku,
                r.Item.Name,
                r.WarehouseId,
                r.Warehouse.Code,
                r.Quantity,
                r.OrderType,
                r.OrderId,
                r.Status.ToString(),
                r.ExpiresAt,
                r.CreatedAt
            ))
            .ToListAsync(ct);
            
        return new PagedResult<ReservationDto>(reservations, page, pageSize, totalCount);
    }
    
    public async Task<Result<ReservationDto>> GetReservationByIdAsync(Guid id, CancellationToken ct = default)
    {
        var reservation = await _context.Reservations
            .AsNoTracking()
            .Include(r => r.Item)
            .Include(r => r.Warehouse)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
            
        if (reservation is null)
            return Result<ReservationDto>.Failure("RESERVATION_NOT_FOUND", "Reservation not found");
            
        return Result<ReservationDto>.Success(MapToDto(reservation));
    }
    
    public async Task<Result<ReservationDto>> CreateReservationAsync(CreateReservationRequest request,
        CancellationToken ct = default)
    {
        // Get current balance
        var balance = await _context.InventoryBalances
            .FirstOrDefaultAsync(b => 
                b.ItemId == request.ItemId && 
                b.WarehouseId == request.WarehouseId && 
                b.LocationId == request.LocationId, ct);
                
        if (balance is null || balance.Available < request.Quantity)
        {
            throw new InsufficientStockException(
                request.ItemId, request.Quantity, balance?.Available ?? 0);
        }
        
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.TenantId,
            ItemId = request.ItemId,
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            Quantity = request.Quantity,
            OrderType = request.OrderType,
            OrderId = request.OrderId,
            Status = ReservationStatus.Active,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = _dateTimeService.UtcNow,
            CreatedBy = _userService.UserName ?? "system"
        };
        
        // Reserve stock
        balance.Reserve(request.Quantity);
        
        // Create ledger entry
        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.TenantId,
            ItemId = request.ItemId,
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            TransactionType = TransactionType.Reserve,
            Quantity = request.Quantity,
            ReferenceType = request.OrderType,
            ReferenceId = request.OrderId,
            TransactionDate = _dateTimeService.UtcNow,
            CreatedAt = _dateTimeService.UtcNow,
            CreatedBy = _userService.UserName ?? "system"
        });
        
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync(ct);
        
        return await GetReservationByIdAsync(reservation.Id, ct);
    }
    
    public async Task<Result<ReservationDto>> ConfirmReservationAsync(Guid id, CancellationToken ct = default)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id, ct);
            
        if (reservation is null)
            return Result<ReservationDto>.Failure("RESERVATION_NOT_FOUND", "Reservation not found");
            
        reservation.Confirm();
        
        // Get balance and issue the stock
        var balance = await _context.InventoryBalances
            .FirstAsync(b => 
                b.ItemId == reservation.ItemId && 
                b.WarehouseId == reservation.WarehouseId && 
                b.LocationId == reservation.LocationId, ct);
                
        balance.Unreserve(reservation.Quantity);
        balance.RemoveStock(reservation.Quantity, Guid.Empty, _dateTimeService.UtcNow);
        
        // Create Issue transaction
        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.TenantId,
            ItemId = reservation.ItemId,
            WarehouseId = reservation.WarehouseId,
            LocationId = reservation.LocationId,
            TransactionType = TransactionType.Issue,
            Quantity = reservation.Quantity,
            ReferenceType = reservation.OrderType,
            ReferenceId = reservation.OrderId,
            TransactionDate = _dateTimeService.UtcNow,
            CreatedAt = _dateTimeService.UtcNow,
            CreatedBy = _userService.UserName ?? "system"
        });
        
        reservation.UpdatedAt = _dateTimeService.UtcNow;
        reservation.UpdatedBy = _userService.UserName;
        
        await _context.SaveChangesAsync(ct);
        
        return await GetReservationByIdAsync(id, ct);
    }
    
    public async Task<Result> ReleaseReservationAsync(Guid id, CancellationToken ct = default)
    {
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id, ct);
            
        if (reservation is null)
            return Result.Failure("RESERVATION_NOT_FOUND", "Reservation not found");
            
        reservation.Release();
        
        // Release reserved stock
        var balance = await _context.InventoryBalances
            .FirstAsync(b => 
                b.ItemId == reservation.ItemId && 
                b.WarehouseId == reservation.WarehouseId && 
                b.LocationId == reservation.LocationId, ct);
                
        balance.Unreserve(reservation.Quantity);
        
        // Create Unreserve transaction
        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.TenantId,
            ItemId = reservation.ItemId,
            WarehouseId = reservation.WarehouseId,
            LocationId = reservation.LocationId,
            TransactionType = TransactionType.Unreserve,
            Quantity = reservation.Quantity,
            ReferenceType = reservation.OrderType,
            ReferenceId = reservation.OrderId,
            TransactionDate = _dateTimeService.UtcNow,
            CreatedAt = _dateTimeService.UtcNow,
            CreatedBy = _userService.UserName ?? "system"
        });
        
        reservation.UpdatedAt = _dateTimeService.UtcNow;
        reservation.UpdatedBy = _userService.UserName;
        
        await _context.SaveChangesAsync(ct);
        
        return Result.Success();
    }
    
    private static ReservationDto MapToDto(Reservation r) => new(
        r.Id,
        r.ItemId,
        r.Item?.Sku ?? "",
        r.Item?.Name ?? "",
        r.WarehouseId,
        r.Warehouse?.Code ?? "",
        r.Quantity,
        r.OrderType,
        r.OrderId,
        r.Status.ToString(),
        r.ExpiresAt,
        r.CreatedAt
    );
}
