using System.Security.Cryptography;
using System.Text;

namespace WebhookReceiver.Tests;

public static class WebhookTestSignature
{
    public static string Sign(string payloadJson, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}