// tests/Orders.IntegrationTests/ApiFactory.cs: the real app + a real SQL Server in a throwaway container
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Infrastructure;
using Testcontainers.MsSql;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Testcontainers 4.x takes the image as a constructor argument
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Orders", _sql.GetConnectionString());
        builder.UseSetting("Webhooks:SigningKey", "test-signing-key");
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sql.DisposeAsync();
        await base.DisposeAsync();
    }
}
