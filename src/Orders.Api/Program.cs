// Program.cs
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.Api.Endpoints;
using Orders.Api.Webhooks;
using Orders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Secrets: in Azure, read Key Vault as this pod's Workload Identity (no password needed).
//    Locally KeyVault:Uri is empty, so this is skipped.
if (builder.Configuration["KeyVault:Uri"] is { Length: > 0 } vaultUri)
{
    builder.Configuration.AddAzureKeyVault(new Uri(vaultUri), new DefaultAzureCredential());
}

// 2. Database: in Azure the connection string uses Entra login and contains no password.
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Orders"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 5)));   // rides out brief Azure SQL failovers

// 3. Telemetry: logs, metrics and traces to Application Insights, when configured.
//    The connection string arrives from Key Vault as AzureMonitor:ConnectionString.
if (!string.IsNullOrEmpty(builder.Configuration["AzureMonitor:ConnectionString"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

// 4. Health: "live" = the process works; "ready" = it can also reach the database.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<OrdersDbContext>(tags: ["ready"]);

builder.Services.AddProblemDetails();                 // consistent JSON error responses
builder.Services.AddSingleton<WebhookSigner>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = c => c.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });
app.MapOrdersEndpoints();

app.Run();

// No "public partial class Program" line: .NET 10 makes the generated Program class public,
// so the integration tests' WebApplicationFactory<Program> can already see it.
