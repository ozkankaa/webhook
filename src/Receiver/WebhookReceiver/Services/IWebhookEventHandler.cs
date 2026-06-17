namespace WebhookReceiver.Services;

public interface IWebhookEventHandler
{
    string EventType { get; }
    Type PayloadType { get; }

    Task HandleAsync(
        object payload,
        string deliveryId,
        CancellationToken cancellationToken);
}