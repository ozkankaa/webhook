using WebhookReceiver.Services;

namespace WebhookReceiver.Tests;

public class HmacSha256WebhookSignatureValidatorTests
{
    [Fact]
    public void IsValid_ReturnsTrue_WhenSignatureMatches()
    {
        var service = new HmacSha256WebhookSignatureValidator();

        var payload = """
        {"orderId":"ORD-1","customerId":"CUST-1","total":10.5}
        """;

        var signature = service.ComputeSignatureForTest(payload, "super-secret");

        var result = service.IsValid(payload, "super-secret", signature);

        Assert.True(result);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenSignatureDoesNotMatch()
    {
        var service = new HmacSha256WebhookSignatureValidator();

        var result = service.IsValid(
            """{"orderId":"ORD-1"}""",
            "super-secret",
            "bad-signature");

        Assert.False(result);
    }
}

internal static class SignatureTestExtensions
{
    public static string ComputeSignatureForTest(
        this HmacSha256WebhookSignatureValidator validator,
        string payload,
        string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(
            System.Text.Encoding.UTF8.GetBytes(secret));

        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}