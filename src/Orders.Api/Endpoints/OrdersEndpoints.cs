// Endpoints/OrdersEndpoints.cs
using Orders.Api.Webhooks;
using Orders.Domain;
using Orders.Infrastructure;

namespace Orders.Api.Endpoints;

public sealed record CreateOrderRequest(Guid CustomerId, decimal Total);

public sealed record OrderResponse(Guid Id, Guid CustomerId, decimal Total, string Status, DateTimeOffset CreatedAt)
{
    public static OrderResponse From(Order o) => new(o.Id, o.CustomerId, o.Total, o.Status.ToString(), o.CreatedAt);
}

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var orders = app.MapGroup("/orders");

        orders.MapGet("/{id:guid}", async (Guid id, OrdersDbContext db, CancellationToken ct) =>
            await db.Orders.FindAsync([id], ct) is { } order
                ? Results.Ok(OrderResponse.From(order))
                : Results.NotFound());

        orders.MapPost("/", async (CreateOrderRequest request, OrdersDbContext db, CancellationToken ct) =>
        {
            if (request.Total <= 0)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["total"] = ["Total must be greater than zero."],
                });

            var order = Order.Create(request.CustomerId, request.Total);
            db.Orders.Add(order);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/orders/{order.Id}", OrderResponse.From(order));
        });

        orders.MapPost("/{id:guid}/refund", async (
            Guid id, OrdersDbContext db, WebhookSigner signer, ILogger<Program> logger, CancellationToken ct) =>
        {
            var order = await db.Orders.FindAsync([id], ct);
            if (order is null) return Results.NotFound();
            if (!order.Refund()) return Results.Conflict(new { error = "Order is already refunded." });

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Order {OrderId} refunded", order.Id);   // structured log: OrderId is searchable
            var signature = signer.Sign($"{order.Id}:{order.Status}");
            return Results.Ok(new { order = OrderResponse.From(order), signature });
        });

        return app;
    }
}
