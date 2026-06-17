using System.ComponentModel.DataAnnotations;

namespace WebhookHub.Domain;

/// <summary>
/// Webhook subscription entity representing a registered callback URL for a specific event type.
/// </summary>
public class WebhookSubscription
{
    /// <summary>
    /// Unique identifier for the webhook subscription.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Event type that this subscription is interested in. When an event with the same eventType is published, a delivery job is created for this subscription.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string EventType { get; set; } = default!;
    /// <summary>
    /// The URL to which the webhook payloads will be delivered.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string CallbackUrl { get; set; } = default!;
    /// <summary>
    /// The secret used to sign the webhook payloads for verification.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string Secret { get; set; } = default!;
    /// <summary>
    /// Indicates whether the webhook subscription is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// The date and time when the webhook subscription was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}