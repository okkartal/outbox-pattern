using FluentEmail.Core;
using Microsoft.EntityFrameworkCore;
using OutboxPattern.WebAPI.Context;

namespace OutboxPattern.WebAPI.Services;

public class OrderOutBoxService : IOrderOutBoxService
{
    private readonly IFluentEmail _fluentEmail;
    private readonly ILogger<OrderOutBoxService> _logger;
    private readonly int _maxRetries = 3;
    private readonly IServiceProvider _serviceProvider;

    public OrderOutBoxService(
        IServiceProvider serviceProvider,
        IFluentEmail fluentEmail,
        ILogger<OrderOutBoxService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _fluentEmail = fluentEmail ?? throw new ArgumentNullException(nameof(fluentEmail));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger)); 
    }

    public async Task ProcessOutboxMessageAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var outboxes = await dbContext.OrderOutBoxes
            .Where(p => !p.IsCompleted && p.Attempt < _maxRetries)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var item in outboxes)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var order = await dbContext.Orders
                    .FirstOrDefaultAsync(p => p.Id == item.OrderId, cancellationToken);

                if (order == null)
                {
                    _logger.LogWarning("Order with ID {OrderId} not found.", item.OrderId);
                    item.IsFailed = true;
                    item.FailMessage = "Order not found";
                    item.IsCompleted = true;
                    item.CompletedDate = DateTimeOffset.UtcNow;
                    continue;
                }

                var body = $"""
                            <h1>Order Status</h1>
                            <p>Product Name: {order.ProductName}</p>
                            <p>Your order date: {order.CreatedAt}</p>
                            """;

                var response = await _fluentEmail
                    .To(order.CustomerEmail)
                    .Subject("Created Order")
                    .Body(body)
                    .SendAsync(cancellationToken);

                if (!response.Successful)
                {
                    item.Attempt++;
                    _logger.LogWarning("Failed to send email to {Email}. Attempt {Attempt}/{MaxRetries}.",
                        order.CustomerEmail, item.Attempt, _maxRetries);
                }
                else
                {
                    item.IsCompleted = true;
                    item.CompletedDate = DateTimeOffset.UtcNow;
                    _logger.LogInformation("Email successfully sent to {Email}.", order.CustomerEmail);
                }
            }
            catch (Exception ex)
            {
                item.Attempt++;
                _logger.LogError(ex,
                    "An error occurred while processing order ID {OrderId}. Attempt {Attempt}/{MaxRetries}.",
                    item.OrderId, item.Attempt, _maxRetries);

                if (item.Attempt >= _maxRetries)
                {
                    item.IsFailed = true;
                    item.IsCompleted = true;
                    item.CompletedDate = DateTimeOffset.UtcNow;
                    item.FailMessage = ex.Message;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
