using System.Text.Json;
using WebhookHub.Domain;
using WebhookHub.Repositories;

namespace WebhookHub.Services;

public class WebhookPublisher : IWebhookPublisher
{
    private readonly IWebhookSubscriptionRepository _subscriptionRepository;
    private readonly IWebhookDeliveryRepository _deliveryRepository;

    public WebhookPublisher(
        IWebhookSubscriptionRepository subscriptionRepository,
        IWebhookDeliveryRepository deliveryRepository)
    {
        _subscriptionRepository = subscriptionRepository;
        _deliveryRepository = deliveryRepository;
    }

    public async Task PublishAsync(string eventType, JsonElement payload, CancellationToken cancellationToken)
    {
        var subscriptions = await _subscriptionRepository
            .GetActiveByEventTypeAsync(eventType, cancellationToken);

        var payloadJson = JsonSerializer.Serialize(payload);

        var deliveries = subscriptions.Select(subscription => new WebhookDelivery
        {
            SubscriptionId = subscription.Id,
            EventType = eventType,
            PayloadJson = payloadJson
        });

        await _deliveryRepository.AddRangeAsync(deliveries, cancellationToken);
    }
}