namespace WebhookHub.Services;

public interface IWebhookSignatureService
{
    string Sign(string payloadJson, string secret);
}