namespace WebhookHub.Domain;

public class WebhookDeliveryAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DeliveryId { get; set; }
    public WebhookDelivery Delivery { get; set; } = default!;

    public int AttemptNumber { get; set; }

    public int? StatusCode { get; set; }
    public bool IsSuccess { get; set; }

    public string? Error { get; set; }

    public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;
}