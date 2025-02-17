using OutboxPattern.WebAPI.Services;

namespace OutboxPattern.WebAPI.BackgroundService;

public sealed class OrderBackgroundService(IServiceProvider serviceProvider) : Microsoft.Extensions.Hosting.BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scoped = serviceProvider.CreateScope();
        var orderOutboxService = scoped.ServiceProvider.GetRequiredService<IOrderOutBoxService>();
        var logger = scoped.ServiceProvider.GetRequiredService<ILogger<OrderBackgroundService>>();
           
        logger.LogInformation("OrderBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await orderOutboxService.ProcessOutboxMessageAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while processing outbox messages.");
            }

            await Task.Delay(2000, stoppingToken);
        }

        logger.LogInformation("OrderBackgroundService is stopping.");
    }
}
