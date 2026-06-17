namespace WebhookHub.Domain;

public enum WebhookDeliveryStatus
{
    Pending,
    Retrying,
    Success,
    DeadLetter
}

public class WebhookDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SubscriptionId { get; set; }
    public WebhookSubscription Subscription { get; set; } = default!;

    public string EventType { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;

    public WebhookDeliveryStatus Status { get; set; } = WebhookDeliveryStatus.Pending;

    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 5;

    public int? LastStatusCode { get; set; }
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public DateTime? DeadLetteredAtUtc { get; set; }

    public DateTime NextAttemptAtUtc { get; set; } = DateTime.UtcNow;

    public List<WebhookDeliveryAttempt> Attempts { get; set; } = new();
}