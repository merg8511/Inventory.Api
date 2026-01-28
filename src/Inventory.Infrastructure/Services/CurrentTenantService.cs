using Inventory.Application.Common.Interfaces;

namespace Inventory.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    private Guid _tenantId;
    
    public Guid TenantId => _tenantId;
    
    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
    }
}
