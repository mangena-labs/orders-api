// tests/Orders.UnitTests/OrderTests.cs: fast, no database
using Orders.Domain;

public class OrderTests
{
    [Fact]
    public void Refund_marks_order_refunded()
    {
        var order = Order.Create(Guid.NewGuid(), 10m);
        Assert.True(order.Refund());
        Assert.Equal(OrderStatus.Refunded, order.Status);
    }

    [Fact]
    public void Refunding_twice_is_rejected()
    {
        var order = Order.Create(Guid.NewGuid(), 10m);
        order.Refund();
        Assert.False(order.Refund());
    }
}
