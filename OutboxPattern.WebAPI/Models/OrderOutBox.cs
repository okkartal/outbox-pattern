namespace OutboxPattern.WebAPI.Models;

public class OrderOutBox
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }
    public string? FailMessage { get; set; }
    public DateTimeOffset? CompletedDate { get; set; }
    public int Attempt { get; set; }
}
