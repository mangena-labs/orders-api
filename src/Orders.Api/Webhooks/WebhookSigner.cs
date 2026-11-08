// Webhooks/WebhookSigner.cs: uses the secret Terraform generated into Key Vault
using System.Security.Cryptography;
using System.Text;

namespace Orders.Api.Webhooks;

public sealed class WebhookSigner(IConfiguration configuration)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(
        configuration["Webhooks:SigningKey"]
        ?? throw new InvalidOperationException("Webhooks:SigningKey is not configured."));

    public string Sign(string payload) =>
        Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload)));
}
