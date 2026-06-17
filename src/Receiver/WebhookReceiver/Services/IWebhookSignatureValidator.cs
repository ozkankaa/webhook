namespace WebhookReceiver.Services;

public interface IWebhookSignatureValidator
{
    bool IsValid(string payloadJson, string secret, string providedSignature);
}