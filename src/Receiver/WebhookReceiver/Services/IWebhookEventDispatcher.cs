namespace WebhookReceiver.Services;

public interface IWebhookEventDispatcher
{
    Task DispatchAsync(
        string eventType,
        string payloadJson,
        string deliveryId,
        CancellationToken cancellationToken);
}