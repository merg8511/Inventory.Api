namespace Inventory.Application.Common.Interfaces;

public interface IDateTimeService
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
}
