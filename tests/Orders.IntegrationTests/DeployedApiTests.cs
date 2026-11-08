// tests/Orders.IntegrationTests/DeployedApiTests.cs: run ONLY against staging, through Front Door
using System.Net;
using System.Net.Http.Json;

[Trait("Category", "Deployed")]
public class DeployedApiTests
{
    private readonly HttpClient _client = new()
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("API_BASE_URL")
                              ?? throw new InvalidOperationException("API_BASE_URL is not set.")),
    };

    [Fact]
    public async Task Order_round_trip_through_the_public_endpoint()
    {
        var created = await _client.PostAsJsonAsync("/orders", new { customerId = Guid.NewGuid(), total = 1.00m });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);

        var fetched = await _client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
    }
}
