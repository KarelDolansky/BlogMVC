namespace BlogMVC.Infrastructure.Interfaces;

public interface IDateTimeProvider
{
    public DateTime Now { get; }
}