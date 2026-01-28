using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using Inventory.Application.Transfers.Dtos;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Transfers;

public interface ITransferService
{
    Task<PagedResult<TransferDto>> GetTransfersAsync(TransferStatus? status, Guid? sourceWarehouseId,
        Guid? destinationWarehouseId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<TransferDto>> GetTransferByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<TransferDto>> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default);
    Task<Result<TransferDto>> CommitTransferAsync(Guid id, CancellationToken ct = default);
    Task<Result<TransferDto>> ShipTransferAsync(Guid id, ShipTransferRequest request, CancellationToken ct = default);
    Task<Result<TransferDto>> ReceiveTransferAsync(Guid id, ReceiveTransferRequest request, CancellationToken ct = default);
    Task<Result> CancelTransferAsync(Guid id, CancellationToken ct = default);
}

public class TransferService : ITransferService
{
    private readonly IInventoryDbContext _context;
    private readonly ICurrentTenantService _tenantService;
    private readonly ICurrentUserService _userService;
    private readonly IDateTimeService _dateTimeService;
    
    public TransferService(
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
    
    public async Task<PagedResult<TransferDto>> GetTransfersAsync(TransferStatus? status,
        Guid? sourceWarehouseId, Guid? destinationWarehouseId, int page, int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Transfers
            .AsNoTracking()
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .Include(t => t.Lines).ThenInclude(l => l.Item)
            .AsQueryable();
            
        if (status.HasValue)
            query = query.Where(t => t.Status == status);
        if (sourceWarehouseId.HasValue)
            query = query.Where(t => t.SourceWarehouseId == sourceWarehouseId);
        if (destinationWarehouseId.HasValue)
            query = query.Where(t => t.DestinationWarehouseId == destinationWarehouseId);
            
        var totalCount = await query.CountAsync(ct);
        
        var transfers = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
            
        return new PagedResult<TransferDto>(
            transfers.Select(MapToDto).ToList(), page, pageSize, totalCount);
    }
    
    public async Task<Result<TransferDto>> GetTransferByIdAsync(Guid id, CancellationToken ct = default)
    {
        var transfer = await _context.Transfers
            .AsNoTracking()
            .Include(t => t.SourceWarehouse)
            .Include(t => t.DestinationWarehouse)
            .Include(t => t.Lines).ThenInclude(l => l.Item)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
            
        if (transfer is null)
            return Result<TransferDto>.Failure("TRANSFER_NOT_FOUND", "Transfer not found");
            
        return Result<TransferDto>.Success(MapToDto(transfer));
    }
    
    public async Task<Result<TransferDto>> CreateTransferAsync(CreateTransferRequest request,
        CancellationToken ct = default)
    {
        if (request.SourceWarehouseId == request.DestinationWarehouseId)
            return Result<TransferDto>.Failure("SELF_TRANSFER_NOT_ALLOWED", 
                "Source and destination warehouses must be different");
                
        // Generate transfer number
        var transferNumber = await GenerateTransferNumberAsync(ct);
        
        var transfer = new Transfer
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.TenantId,
            TransferNumber = transferNumber,
            SourceWarehouseId = request.SourceWarehouseId,
            SourceLocationId = request.SourceLocationId,
            DestinationWarehouseId = request.DestinationWarehouseId,
            DestinationLocationId = request.DestinationLocationId,
            Status = TransferStatus.Draft,
            Notes = request.Notes,
            CreatedAt = _dateTimeService.UtcNow,
            CreatedBy = _userService.UserName ?? "system"
        };
        
        foreach (var lineRequest in request.Lines)
        {
            transfer.Lines.Add(new TransferLine
            {
                Id = Guid.NewGuid(),
                TransferId = transfer.Id,
                ItemId = lineRequest.ItemId,
                RequestedQuantity = lineRequest.RequestedQuantity,
                LotNumber = lineRequest.LotNumber,
                SerialNumber = lineRequest.SerialNumber
            });
        }
        
        _context.Transfers.Add(transfer);
        await _context.SaveChangesAsync(ct);
        
        // Reload with navigation
        return await GetTransferByIdAsync(transfer.Id, ct);
    }
    
    public async Task<Result<TransferDto>> CommitTransferAsync(Guid id, CancellationToken ct = default)
    {
        var transfer = await _context.Transfers
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
            
        if (transfer is null)
            return Result<TransferDto>.Failure("TRANSFER_NOT_FOUND", "Transfer not found");
            
        // Validate and reserve stock for each line
        foreach (var line in transfer.Lines)
        {
            var balance = await _context.InventoryBalances
                .FirstOrDefaultAsync(b => 
                    b.ItemId == line.ItemId && 
                    b.WarehouseId == transfer.SourceWarehouseId &&
                    b.LocationId == transfer.SourceLocationId, ct);
                    
            if (balance is null || balance.Available < line.RequestedQuantity)
            {
                throw new InsufficientStockException(
                    line.ItemId, line.RequestedQuantity, balance?.Available ?? 0);
            }
            
            balance.Reserve(line.RequestedQuantity);
        }
        
        transfer.Commit(_dateTimeService.UtcNow);
        
        await _context.SaveChangesAsync(ct);
        
        return await GetTransferByIdAsync(id, ct);
    }
    
    public async Task<Result<TransferDto>> ShipTransferAsync(Guid id, ShipTransferRequest request,
        CancellationToken ct = default)
    {
        var transfer = await _context.Transfers
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
            
        if (transfer is null)
            return Result<TransferDto>.Failure("TRANSFER_NOT_FOUND", "Transfer not found");
            
        transfer.Ship(_dateTimeService.UtcNow);
        
        foreach (var shipLine in request.Lines)
        {
            var line = transfer.Lines.FirstOrDefault(l => l.Id == shipLine.LineId);
            if (line is not null)
            {
                line.ShippedQuantity = shipLine.ShippedQuantity;
                
                // Create TransferOut transaction and update balances
                var balance = await _context.InventoryBalances
                    .FirstAsync(b => 
                        b.ItemId == line.ItemId && 
                        b.WarehouseId == transfer.SourceWarehouseId, ct);
                        
                balance.Unreserve(line.RequestedQuantity);
                balance.RemoveStock(shipLine.ShippedQuantity, Guid.Empty, _dateTimeService.UtcNow);
                
                // Add InTransit at destination
                var destBalance = await GetOrCreateBalanceAsync(
                    line.ItemId, transfer.DestinationWarehouseId, transfer.DestinationLocationId, ct);
                destBalance.AddInTransit(shipLine.ShippedQuantity);
                
                // Create ledger entry
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantService.TenantId,
                    ItemId = line.ItemId,
                    WarehouseId = transfer.SourceWarehouseId,
                    TransactionType = TransactionType.TransferOut,
                    Quantity = shipLine.ShippedQuantity,
                    ReferenceType = "Transfer",
                    ReferenceId = transfer.TransferNumber,
                    TransactionDate = _dateTimeService.UtcNow,
                    CreatedAt = _dateTimeService.UtcNow,
                    CreatedBy = _userService.UserName ?? "system"
                });
            }
        }
        
        await _context.SaveChangesAsync(ct);
        
        return await GetTransferByIdAsync(id, ct);
    }
    
    public async Task<Result<TransferDto>> ReceiveTransferAsync(Guid id, ReceiveTransferRequest request,
        CancellationToken ct = default)
    {
        var transfer = await _context.Transfers
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
            
        if (transfer is null)
            return Result<TransferDto>.Failure("TRANSFER_NOT_FOUND", "Transfer not found");
            
        transfer.Receive(_dateTimeService.UtcNow);
        
        foreach (var receiveLine in request.Lines)
        {
            var line = transfer.Lines.FirstOrDefault(l => l.Id == receiveLine.LineId);
            if (line is not null)
            {
                line.ReceivedQuantity = receiveLine.ReceivedQuantity;
                
                // Update destination balance
                var destBalance = await _context.InventoryBalances
                    .FirstAsync(b => 
                        b.ItemId == line.ItemId && 
                        b.WarehouseId == transfer.DestinationWarehouseId, ct);
                        
                destBalance.RemoveInTransit(line.ShippedQuantity ?? receiveLine.ReceivedQuantity);
                destBalance.AddStock(receiveLine.ReceivedQuantity, Guid.Empty, _dateTimeService.UtcNow);
                
                // Create ledger entry
                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantService.TenantId,
                    ItemId = line.ItemId,
                    WarehouseId = transfer.DestinationWarehouseId,
                    TransactionType = TransactionType.TransferIn,
                    Quantity = receiveLine.ReceivedQuantity,
                    ReferenceType = "Transfer",
                    ReferenceId = transfer.TransferNumber,
                    TransactionDate = _dateTimeService.UtcNow,
                    CreatedAt = _dateTimeService.UtcNow,
                    CreatedBy = _userService.UserName ?? "system"
                });
            }
        }
        
        await _context.SaveChangesAsync(ct);
        
        return await GetTransferByIdAsync(id, ct);
    }
    
    public async Task<Result> CancelTransferAsync(Guid id, CancellationToken ct = default)
    {
        var transfer = await _context.Transfers
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
            
        if (transfer is null)
            return Result.Failure("TRANSFER_NOT_FOUND", "Transfer not found");
            
        // If committed, release reservations
        if (transfer.Status == TransferStatus.Committed)
        {
            foreach (var line in transfer.Lines)
            {
                var balance = await _context.InventoryBalances
                    .FirstOrDefaultAsync(b => 
                        b.ItemId == line.ItemId && 
                        b.WarehouseId == transfer.SourceWarehouseId, ct);
                        
                balance?.Unreserve(line.RequestedQuantity);
            }
        }
        
        transfer.Cancel(_dateTimeService.UtcNow);
        
        await _context.SaveChangesAsync(ct);
        
        return Result.Success();
    }
    
    private async Task<string> GenerateTransferNumberAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prefix = $"TRF-{today:yyyyMMdd}";
        
        var lastNumber = await _context.Transfers
            .Where(t => t.TransferNumber.StartsWith(prefix))
            .OrderByDescending(t => t.TransferNumber)
            .Select(t => t.TransferNumber)
            .FirstOrDefaultAsync(ct);
            
        int sequence = 1;
        if (lastNumber is not null)
        {
            var parts = lastNumber.Split('-');
            if (parts.Length > 2 && int.TryParse(parts[2], out var lastSeq))
            {
                sequence = lastSeq + 1;
            }
        }
        
        return $"{prefix}-{sequence:D4}";
    }
    
    private async Task<InventoryBalance> GetOrCreateBalanceAsync(Guid itemId, Guid warehouseId,
        Guid? locationId, CancellationToken ct)
    {
        var balance = await _context.InventoryBalances
            .FirstOrDefaultAsync(b => 
                b.ItemId == itemId && 
                b.WarehouseId == warehouseId && 
                b.LocationId == locationId, ct);
                
        if (balance is null)
        {
            balance = new InventoryBalance
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantService.TenantId,
                ItemId = itemId,
                WarehouseId = warehouseId,
                LocationId = locationId,
                OnHand = 0,
                Reserved = 0,
                InTransit = 0,
                UpdatedAt = _dateTimeService.UtcNow
            };
            _context.InventoryBalances.Add(balance);
        }
        
        return balance;
    }
    
    private static TransferDto MapToDto(Transfer t) => new(
        t.Id,
        t.TransferNumber,
        t.SourceWarehouseId,
        t.SourceWarehouse?.Code ?? "",
        t.DestinationWarehouseId,
        t.DestinationWarehouse?.Code ?? "",
        t.Status.ToString(),
        t.Notes,
        t.CommittedAt,
        t.ShippedAt,
        t.ReceivedAt,
        t.CreatedAt,
        t.Lines.Select(l => new TransferLineDto(
            l.Id,
            l.ItemId,
            l.Item?.Sku ?? "",
            l.Item?.Name ?? "",
            l.RequestedQuantity,
            l.ShippedQuantity,
            l.ReceivedQuantity,
            l.LotNumber
        )).ToList()
    );
}
