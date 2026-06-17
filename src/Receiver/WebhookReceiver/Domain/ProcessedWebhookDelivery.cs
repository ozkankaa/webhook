namespace WebhookReceiver.Domain;

public class ProcessedWebhookDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string DeliveryId { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string PayloadHash { get; set; } = default!;

    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}