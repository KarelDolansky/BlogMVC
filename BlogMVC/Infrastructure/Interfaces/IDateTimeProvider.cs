namespace BlogMVC.Infrastructure.Interfaces;

/// <summary>
///     Abstraction over the current time. Allows the system clock (DateTime.UtcNow)
///     to be replaced with a predictable value via a mock in tests.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>The current date and time.</summary>
    public DateTime Now { get; }
}