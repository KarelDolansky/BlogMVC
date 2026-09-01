using BlogMVC.Infrastructure.Interfaces;

namespace BlogMVC.Infrastructure.Providers;

/// <summary>
///     Production implementation of <see cref="IDateTimeProvider"/> that returns the real
///     current time in UTC. Replaced with a mock in tests so time values are deterministic.
/// </summary>
public class SystemDateTimeProvider : IDateTimeProvider
{
    /// <summary>Returns <see cref="DateTime.UtcNow" />.</summary>
    public DateTime Now => DateTime.UtcNow;
}