using BlogMVC.Infrastructure.Interfaces;

namespace BlogMVC.Infrastructure.Providers;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime Now => DateTime.UtcNow;
}