namespace OutboxPattern.WebAPI.Services;

public interface IOrderOutBoxService
{
    Task ProcessOutboxMessageAsync(CancellationToken cancellationToken);
}
