using System.Security.Cryptography;
using System.Text;

namespace WebhookHub.Services;

public class HmacSha256WebhookSignatureService : IWebhookSignatureService
{
    public string Sign(string payloadJson, string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}