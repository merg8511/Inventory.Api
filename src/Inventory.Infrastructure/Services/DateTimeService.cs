using Inventory.Application.Common.Interfaces;

namespace Inventory.Infrastructure.Services;

public class DateTimeService : IDateTimeService
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
