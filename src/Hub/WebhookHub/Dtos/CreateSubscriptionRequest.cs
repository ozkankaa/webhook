using System.ComponentModel.DataAnnotations;

namespace WebhookHub.Dtos;

/// <summary>
/// Contains the information required to create a new webhook subscription, including the event type, callback URL, and secret for signing payloads.
/// </summary>
/// <param name="EventType">The type of event the subscription is interested in.</param>
/// <param name="CallbackUrl">The URL to which the webhook payloads will be delivered.</param>
/// <param name="Secret">The secret used to sign the webhook payloads for verification.</param>
public record CreateSubscriptionRequest(
    /// <summary>
    /// The type of event the subscription is interested in.
    /// </summary>
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    string EventType,
    /// <summary>
    /// The URL to which the webhook payloads will be delivered.
    /// </summary>
    [Required]
    [Url]
    [MaxLength(500)]
    string CallbackUrl,
    /// <summary>
    /// The secret used to sign the webhook payloads for verification.
    /// </summary>
    [Required]
    [MinLength(16)]
    [MaxLength(200)]
    string Secret
);