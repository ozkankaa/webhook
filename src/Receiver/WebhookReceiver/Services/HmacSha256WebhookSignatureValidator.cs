using System.Security.Cryptography;
using System.Text;

namespace WebhookReceiver.Services;

public class HmacSha256WebhookSignatureValidator : IWebhookSignatureValidator
{
    public bool IsValid(string payloadJson, string secret, string providedSignature)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

        using var hmac = new HMACSHA256(secretBytes);
        var hash = hmac.ComputeHash(payloadBytes);

        var expectedSignature = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(providedSignature.ToLowerInvariant()));
    }
}