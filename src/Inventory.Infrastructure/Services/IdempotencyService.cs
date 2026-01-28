using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Services;

public interface IIdempotencyService
{
    Task<IdempotencyKey?> GetAsync(string key, CancellationToken ct = default);
    Task<string> ComputeHashAsync(object request);
    Task SaveAsync(string key, string requestHash, int statusCode, object response, CancellationToken ct = default);
}

public class IdempotencyService : IIdempotencyService
{
    private readonly IInventoryDbContext _context;
    private readonly ICurrentTenantService _tenantService;
    private readonly IDateTimeService _dateTimeService;
    private readonly int _ttlHours;
    
    public IdempotencyService(
        IInventoryDbContext context,
        ICurrentTenantService tenantService,
        IDateTimeService dateTimeService,
        int ttlHours = 24)
    {
        _context = context;
        _tenantService = tenantService;
        _dateTimeService = dateTimeService;
        _ttlHours = ttlHours;
    }
    
    public async Task<IdempotencyKey?> GetAsync(string key, CancellationToken ct = default)
    {
        return await _context.IdempotencyKeys
            .FirstOrDefaultAsync(k => k.Key == key && k.ExpiresAt > _dateTimeService.UtcNow, ct);
    }
    
    public Task<string> ComputeHashAsync(object request)
    {
        var json = JsonSerializer.Serialize(request);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Task.FromResult(Convert.ToHexString(bytes));
    }
    
    public async Task SaveAsync(string key, string requestHash, int statusCode, object response, 
        CancellationToken ct = default)
    {
        var idempotencyKey = new IdempotencyKey
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantService.TenantId,
            Key = key,
            RequestHash = requestHash,
            ResponseStatusCode = statusCode,
            ResponseBody = JsonSerializer.Serialize(response),
            CreatedAt = _dateTimeService.UtcNow,
            ExpiresAt = _dateTimeService.UtcNow.AddHours(_ttlHours)
        };
        
        _context.IdempotencyKeys.Add(idempotencyKey);
        await _context.SaveChangesAsync(ct);
    }
}
