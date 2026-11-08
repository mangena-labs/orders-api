// Order.cs
namespace Orders.Domain;

public enum OrderStatus { Placed, Refunded }

public sealed class Order
{
    private Order() { }                      // used by EF Core when loading from the database

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Order Create(Guid customerId, decimal total) => new()
    {
        Id = Guid.CreateVersion7(),          // time-ordered IDs are friendlier to database indexes
        CustomerId = customerId,
        Total = total,
        Status = OrderStatus.Placed,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    /// <returns>false if the order was already refunded.</returns>
    public bool Refund()
    {
        if (Status == OrderStatus.Refunded) return false;
        Status = OrderStatus.Refunded;
        return true;
    }
}
