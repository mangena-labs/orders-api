// tests/Orders.IntegrationTests/OrdersApiTests.cs
using System.Net;
using System.Net.Http.Json;

public class OrdersApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private sealed record OrderDto(Guid Id, string Status);

    [Fact]
    public async Task Create_then_refund_an_order()
    {
        var client = factory.CreateClient();

        var created = await client.PostAsJsonAsync("/orders", new { customerId = Guid.NewGuid(), total = 42.50m });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var order = await created.Content.ReadFromJsonAsync<OrderDto>();

        var refund = await client.PostAsync($"/orders/{order!.Id}/refund", null);
        Assert.Equal(HttpStatusCode.OK, refund.StatusCode);

        var again = await client.PostAsync($"/orders/{order.Id}/refund", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Ready_endpoint_reports_healthy_when_database_is_up()
    {
        var response = await factory.CreateClient().GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
