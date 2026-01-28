namespace Inventory.Domain.Exceptions;

public class ConcurrencyConflictException : DomainException
{
    public string EntityType { get; }
    public Guid EntityId { get; }
    
    public ConcurrencyConflictException(string entityType, Guid entityId)
        : base("CONCURRENCY_CONFLICT", 
            $"The {entityType} was modified by another user. Please refresh and try again.")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}
