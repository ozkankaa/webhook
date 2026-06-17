namespace WebhookReceiver.Services;

public interface IProcessedDeliveryStore
{
    Task<bool> HasProcessedAsync(string deliveryId, CancellationToken cancellationToken);
    Task MarkProcessedAsync(
        string deliveryId,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken);
}