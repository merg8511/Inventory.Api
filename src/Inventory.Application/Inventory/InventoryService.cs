using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using Inventory.Application.Inventory.Dtos;
using Inventory.Domain.Entities;
using Inventory.Domain.Enums;
using Inventory.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Inventory;

public interface IInventoryService
{
    Task<PagedResult<BalanceDto>> GetBalancesAsync(Guid? itemId, Guid? warehouseId, 
        int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<TransactionDto>> GetTransactionsAsync(Guid? itemId, Guid? warehouseId,
        DateTimeOffset? fromDate, DateTimeOffset? toDate, TransactionType? transactionType,
        int page, int pageSize, CancellationToken ct = default);
    Task<Result<InventoryOperationResult>> ReceiptAsync(ReceiptRequest request, 
        string? idempotencyKey, CancellationToken ct = default);
    Task<Result<InventoryOperationResult>> IssueAsync(IssueRequest request, 
        string? idempotencyKey, CancellationToken ct = default);
    Task<Result<InventoryOperationResult>> AdjustmentAsync(AdjustmentRequest request, 
        string? idempotencyKey, CancellationToken ct = default);
}

public class InventoryService : IInventoryService
{
    private readonly IInventoryDbContext _context;
    private readonly ICurrentTenantService _tenantService;
    private readonly ICurrentUserService _userService;
    private readonly IDateTimeService _dateTimeService;
    private readonly bool _allowNegativeStock;
    
    public InventoryService(
        IInventoryDbContext context,
        ICurrentTenantService tenantService,
        ICurrentUserService userService,
        IDateTimeService dateTimeService,
        bool allowNegativeStock = false)
    {
        _context = context;
        _tenantService = tenantService;
        _userService = userService;
        _dateTimeService = dateTimeService;
        _allowNegativeStock = allowNegativeStock;
    }
    
    public async Task<PagedResult<BalanceDto>> GetBalancesAsync(Guid? itemId, Guid? warehouseId,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.InventoryBalances
            .AsNoTracking()
            .Include(b => b.Item)
            .Include(b => b.Warehouse)
            .Include(b => b.Location)
            .AsQueryable();
            
        if (itemId.HasValue)
            query = query.Where(b => b.ItemId == itemId);
            
        if (warehouseId.HasValue)
            query = query.Where(b => b.WarehouseId == warehouseId);
            
        var totalCount = await query.CountAsync(ct);
        
        var balances = await query
            .OrderBy(b => b.Item.Sku)
            .ThenBy(b => b.Warehouse.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BalanceDto(
                b.Id,
                b.ItemId,
                b.Item.Sku,
                b.Item.Name,
                b.WarehouseId,
                b.Warehouse.Code,
                b.LocationId,
                b.Location != null ? b.Location.Code : null,
                b.OnHand,
                b.Reserved,
                b.OnHand - b.Reserved,
                b.InTransit,
                b.LastTransactionDate
            ))
            .ToListAsync(ct);
            
        return new PagedResult<BalanceDto>(balances, page, pageSize, totalCount);
    }
    
    public async Task<PagedResult<TransactionDto>> GetTransactionsAsync(Guid? itemId, Guid? warehouseId,
        DateTimeOffset? fromDate, DateTimeOffset? toDate, TransactionType? transactionType,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.InventoryTransactions
            .AsNoTracking()
            .Include(t => t.Item)
            .Include(t => t.Warehouse)
            .AsQueryable();
            
        if (itemId.HasValue)
            query = query.Where(t => t.ItemId == itemId);
            
        if (warehouseId.HasValue)
            query = query.Where(t => t.WarehouseId == warehouseId);
            
        if (fromDate.HasValue)
            query = query.Where(t => t.TransactionDate >= fromDate);
            
        if (toDate.HasValue)
            query = query.Where(t => t.TransactionDate <= toDate);
            
        if (transactionType.HasValue)
            query = query.Where(t => t.TransactionType == transactionType);
            
        var totalCount = await query.CountAsync(ct);
        
        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
                t.Id,
                t.ItemId,
                t.Item.Sku,
                t.WarehouseId,
                t.Warehouse.Code,
                t.TransactionType.ToString(),
                t.Quantity,
                t.UnitCost,
                t.TotalCost,
                t.ReferenceType,
                t.ReferenceId,
                t.ReasonCode,
                t.ReasonDescription,
                t.TransactionDate,
                t.CreatedBy
            ))
            .ToListAsync(ct);
            
        return new PagedResult<TransactionDto>(transactions, page, pageSize, totalCount);
    }
    
    public async Task<Result<InventoryOperationResult>> ReceiptAsync(ReceiptRequest request,
        string? idempotencyKey, CancellationToken ct = default)
    {
        // Validate item and warehouse exist
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null)
            return Result<InventoryOperationResult>.Failure("ITEM_NOT_FOUND", "Item not found");
            
        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId, ct);
        if (warehouse is null)
            return Result<InventoryOperationResult>.Failure("WAREHOUSE_NOT_FOUND", "Warehouse not found");
            
        var now = _dateTimeService.UtcNow;
        var transactionDate = request.TransactionDate ?? now;
        
        // Get or create balance
        var balance = await GetOrCreateBalanceAsync(request.ItemId, request.WarehouseId, request.LocationId, ct);
        
        // Create transaction
        var transaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.TenantId,
            ItemId = request.ItemId,
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            TransactionType = TransactionType.Receipt,
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            TotalCost = request.UnitCost.HasValue ? request.UnitCost.Value * request.Quantity : null,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            LotNumber = request.LotNumber,
            ExpirationDate = request.ExpirationDate,
            TransactionDate = transactionDate,
            CreatedAt = now,
            CreatedBy = _userService.UserName ?? "system",
            IdempotencyKey = idempotencyKey
        };
        
        _context.InventoryTransactions.Add(transaction);
        
        // Update balance
        balance.AddStock(request.Quantity, transaction.Id, transactionDate);
        
        await _context.SaveChangesAsync(ct);
        
        return Result<InventoryOperationResult>.Success(new InventoryOperationResult(
            transaction.Id,
            new BalanceSnapshotDto(balance.OnHand, balance.Reserved, balance.Available, balance.InTransit)
        ));
    }
    
    public async Task<Result<InventoryOperationResult>> IssueAsync(IssueRequest request,
        string? idempotencyKey, CancellationToken ct = default)
    {
        // Validate item and warehouse exist
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null)
            return Result<InventoryOperationResult>.Failure("ITEM_NOT_FOUND", "Item not found");
            
        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId, ct);
        if (warehouse is null)
            return Result<InventoryOperationResult>.Failure("WAREHOUSE_NOT_FOUND", "Warehouse not found");
            
        // Get balance - must exist for issue
        var balance = await _context.InventoryBalances
            .FirstOrDefaultAsync(b => 
                b.ItemId == request.ItemId && 
                b.WarehouseId == request.WarehouseId && 
                b.LocationId == request.LocationId, ct);
                
        if (balance is null)
            return Result<InventoryOperationResult>.Failure("NO_STOCK", "No stock exists for this item/warehouse combination");
            
        // Check available stock
        if (!_allowNegativeStock && balance.Available < request.Quantity)
        {
            throw new InsufficientStockException(request.ItemId, request.Quantity, balance.Available);
        }
        
        var now = _dateTimeService.UtcNow;
        var transactionDate = request.TransactionDate ?? now;
        
        // Create transaction
        var transaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.TenantId,
            ItemId = request.ItemId,
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            TransactionType = TransactionType.Issue,
            Quantity = request.Quantity,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            TransactionDate = transactionDate,
            CreatedAt = now,
            CreatedBy = _userService.UserName ?? "system",
            IdempotencyKey = idempotencyKey
        };
        
        _context.InventoryTransactions.Add(transaction);
        
        // Update balance
        balance.RemoveStock(request.Quantity, transaction.Id, transactionDate, _allowNegativeStock);
        
        await _context.SaveChangesAsync(ct);
        
        return Result<InventoryOperationResult>.Success(new InventoryOperationResult(
            transaction.Id,
            new BalanceSnapshotDto(balance.OnHand, balance.Reserved, balance.Available, balance.InTransit)
        ));
    }
    
    public async Task<Result<InventoryOperationResult>> AdjustmentAsync(AdjustmentRequest request,
        string? idempotencyKey, CancellationToken ct = default)
    {
        // Validate item and warehouse exist
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == request.ItemId, ct);
        if (item is null)
            return Result<InventoryOperationResult>.Failure("ITEM_NOT_FOUND", "Item not found");
            
        var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == request.WarehouseId, ct);
        if (warehouse is null)
            return Result<InventoryOperationResult>.Failure("WAREHOUSE_NOT_FOUND", "Warehouse not found");
            
        var isIncrease = request.AdjustmentType.Equals("increase", StringComparison.OrdinalIgnoreCase);
        
        // Get or create balance
        var balance = await GetOrCreateBalanceAsync(request.ItemId, request.WarehouseId, request.LocationId, ct);
        
        // For decrease, check stock
        if (!isIncrease && !_allowNegativeStock && balance.Available < request.Quantity)
        {
            throw new InsufficientStockException(request.ItemId, request.Quantity, balance.Available);
        }
        
        var now = _dateTimeService.UtcNow;
        var transactionDate = request.TransactionDate ?? now;
        
        var transactionType = isIncrease 
            ? TransactionType.PositiveAdjustment 
            : TransactionType.NegativeAdjustment;
        
        // Create transaction
        var transaction = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.TenantId,
            ItemId = request.ItemId,
            WarehouseId = request.WarehouseId,
            LocationId = request.LocationId,
            TransactionType = transactionType,
            Quantity = request.Quantity,
            ReasonCode = request.ReasonCode,
            ReasonDescription = request.ReasonDescription,
            TransactionDate = transactionDate,
            CreatedAt = now,
            CreatedBy = _userService.UserName ?? "system",
            IdempotencyKey = idempotencyKey
        };
        
        _context.InventoryTransactions.Add(transaction);
        
        // Update balance
        if (isIncrease)
        {
            balance.AddStock(request.Quantity, transaction.Id, transactionDate);
        }
        else
        {
            balance.RemoveStock(request.Quantity, transaction.Id, transactionDate, _allowNegativeStock);
        }
        
        await _context.SaveChangesAsync(ct);
        
        return Result<InventoryOperationResult>.Success(new InventoryOperationResult(
            transaction.Id,
            new BalanceSnapshotDto(balance.OnHand, balance.Reserved, balance.Available, balance.InTransit)
        ));
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
}
