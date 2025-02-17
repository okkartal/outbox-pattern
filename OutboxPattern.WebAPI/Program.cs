using Mapster;
using Microsoft.EntityFrameworkCore;
using OutboxPattern.WebAPI.BackgroundService;
using OutboxPattern.WebAPI.Context;
using OutboxPattern.WebAPI.Dtos;
using OutboxPattern.WebAPI.Models;
using OutboxPattern.WebAPI.Services;
using Scalar.AspNetCore;
using TS.Result;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});

builder.Services.AddFluentEmail("info@outboxpattern.com", "Outbox Pattern API")
    .AddSmtpSender("localhost", 25);

builder.Services.AddScoped<IOrderOutBoxService, OrderOutBoxService>()
    .AddHostedService<OrderBackgroundService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapScalarApiReference();
app.UseHttpsRedirection();

app.UseCors(x => x.AllowAnyMethod()
    .AllowAnyHeader()
    .AllowAnyOrigin());
app.MapPost("orders", async (CreateOrderDto request, ApplicationDbContext dbContext,
        CancellationToken cancellationToken) =>
    {
        var order = request.Adapt<Order>();
        order.CreatedAt = DateTimeOffset.Now;
        dbContext.Orders.Add(order);

        OrderOutBox orderOutBox = new()
        {
            OrderId = order.Id,
            CreatedAt = DateTimeOffset.Now
        };
        dbContext.Add(orderOutBox);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(Result<string>.Succeed("Order successfully created"));
    })
    .Produces<Result<string>>();

app.MapGet("orders", async (ApplicationDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var orders = await dbContext.Orders.ToListAsync(cancellationToken);
    return Results.Ok(orders);
}).Produces<List<Order>>();

app.Run();
