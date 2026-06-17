using System.Collections.Concurrent;

namespace WebhookReceiver.Services;

public class InMemoryProcessedDeliveryStore : IProcessedDeliveryStore
{
    private readonly ConcurrentDictionary<string, DateTime> _processedDeliveries = new();

    public Task<bool> HasProcessedAsync(string deliveryId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_processedDeliveries.ContainsKey(deliveryId));
    }

    public Task MarkProcessedAsync(string deliveryId, string eventType, string payloadJson, CancellationToken cancellationToken)
    {
        _processedDeliveries.TryAdd(deliveryId, DateTime.UtcNow);
        return Task.CompletedTask;
    }
}