using Inventory.Application.Common.Interfaces;
using Inventory.Application.Common.Models;
using Inventory.Application.Items.Dtos;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Items;

public interface IItemsService
{
    Task<PagedResult<ItemListDto>> GetItemsAsync(int page, int pageSize, string? search, 
        Guid? categoryId, bool? isActive, CancellationToken ct = default);
    Task<Result<ItemDto>> GetItemByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<ItemDto>> CreateItemAsync(CreateItemRequest request, CancellationToken ct = default);
    Task<Result<ItemDto>> UpdateItemAsync(Guid id, UpdateItemRequest request, CancellationToken ct = default);
    Task<Result> DeactivateItemAsync(Guid id, CancellationToken ct = default);
}

public class ItemsService : IItemsService
{
    private readonly IInventoryDbContext _context;
    private readonly ICurrentTenantService _tenantService;
    private readonly ICurrentUserService _userService;
    private readonly IDateTimeService _dateTimeService;
    
    public ItemsService(
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
    
    public async Task<PagedResult<ItemListDto>> GetItemsAsync(int page, int pageSize, string? search,
        Guid? categoryId, bool? isActive, CancellationToken ct = default)
    {
        var query = _context.Items.AsNoTracking();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Sku.Contains(search) || i.Name.Contains(search));
        }
        
        if (categoryId.HasValue)
        {
            query = query.Where(i => i.CategoryId == categoryId);
        }
        
        if (isActive.HasValue)
        {
            query = query.Where(i => i.IsActive == isActive.Value);
        }
        
        var totalCount = await query.CountAsync(ct);
        
        var items = await query
            .OrderBy(i => i.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new ItemListDto(
                i.Id,
                i.Sku,
                i.Name,
                i.Category != null ? i.Category.Name : null,
                i.UnitOfMeasure.Code,
                i.SalePrice,
                i.IsActive
            ))
            .ToListAsync(ct);
            
        return new PagedResult<ItemListDto>(items, page, pageSize, totalCount);
    }
    
    public async Task<Result<ItemDto>> GetItemByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _context.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.UnitOfMeasure)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
            
        if (item is null)
        {
            return Result<ItemDto>.Failure("ITEM_NOT_FOUND", $"Item with ID {id} was not found");
        }
        
        return Result<ItemDto>.Success(MapToDto(item));
    }
    
    public async Task<Result<ItemDto>> CreateItemAsync(CreateItemRequest request, CancellationToken ct = default)
    {
        // Check for duplicate SKU
        var existingItem = await _context.Items
            .FirstOrDefaultAsync(i => i.Sku == request.Sku, ct);
            
        if (existingItem is not null)
        {
            return Result<ItemDto>.Failure("SKU_ALREADY_EXISTS", $"SKU '{request.Sku}' is already in use");
        }
        
        // Verify UoM exists
        var uomExists = await _context.UnitsOfMeasure.AnyAsync(u => u.Id == request.UnitOfMeasureId, ct);
        if (!uomExists)
        {
            return Result<ItemDto>.Failure("UOM_NOT_FOUND", "Unit of Measure not found");
        }
        
        var item = new Item
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.TenantId,
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId,
            UnitOfMeasureId = request.UnitOfMeasureId,
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            TrackingType = request.TrackingType,
            MinimumStock = request.MinimumStock,
            IsActive = true,
            CreatedAt = _dateTimeService.UtcNow,
            CreatedBy = _userService.UserName ?? "system"
        };
        
        _context.Items.Add(item);
        await _context.SaveChangesAsync(ct);
        
        // Reload with navigation properties
        await _context.Items.Entry(item).Reference(i => i.UnitOfMeasure).LoadAsync(ct);
        if (item.CategoryId.HasValue)
        {
            await _context.Items.Entry(item).Reference(i => i.Category).LoadAsync(ct);
        }
        
        return Result<ItemDto>.Success(MapToDto(item));
    }
    
    public async Task<Result<ItemDto>> UpdateItemAsync(Guid id, UpdateItemRequest request, CancellationToken ct = default)
    {
        var item = await _context.Items
            .Include(i => i.Category)
            .Include(i => i.UnitOfMeasure)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
            
        if (item is null)
        {
            return Result<ItemDto>.Failure("ITEM_NOT_FOUND", $"Item with ID {id} was not found");
        }
        
        if (item.RowVersion != request.RowVersion)
        {
            throw new ConcurrencyConflictException("Item", id);
        }
        
        item.Name = request.Name;
        item.Description = request.Description;
        item.CategoryId = request.CategoryId;
        item.UnitOfMeasureId = request.UnitOfMeasureId;
        item.CostPrice = request.CostPrice;
        item.SalePrice = request.SalePrice;
        item.MinimumStock = request.MinimumStock;
        item.UpdatedAt = _dateTimeService.UtcNow;
        item.UpdatedBy = _userService.UserName;
        item.RowVersion++;
        
        await _context.SaveChangesAsync(ct);
        
        return Result<ItemDto>.Success(MapToDto(item));
    }
    
    public async Task<Result> DeactivateItemAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id, ct);
        
        if (item is null)
        {
            return Result.Failure("ITEM_NOT_FOUND", $"Item with ID {id} was not found");
        }
        
        item.IsActive = false;
        item.UpdatedAt = _dateTimeService.UtcNow;
        item.UpdatedBy = _userService.UserName;
        
        await _context.SaveChangesAsync(ct);
        
        return Result.Success();
    }
    
    private static ItemDto MapToDto(Item item) => new(
        item.Id,
        item.Sku,
        item.Name,
        item.Description,
        item.CategoryId,
        item.Category?.Name,
        item.UnitOfMeasureId,
        item.UnitOfMeasure.Code,
        item.CostPrice,
        item.SalePrice,
        item.TrackingType.ToString(),
        item.MinimumStock,
        item.IsActive,
        item.CreatedAt
    );
}
