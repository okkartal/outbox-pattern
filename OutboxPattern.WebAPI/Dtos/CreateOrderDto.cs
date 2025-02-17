namespace OutboxPattern.WebAPI.Dtos;

public abstract record CreateOrderDto(
    string ProductName,
    int Quantity,
    decimal Price,
    string CustomerEmail);
    