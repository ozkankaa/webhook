using System.ComponentModel.DataAnnotations;

namespace WebhookReceiver.Models;

/// <summary>
/// Payload for order.cancelled events.
/// </summary>
public class OrderCancelledPayload
{
    /// <summary>
    /// Unique order identifier.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string OrderId { get; set; } = default!;

    /// <summary>
    /// Reason for the order cancellation.
    /// </summary>
    /// <example>Customer requested cancellation</example>
    [Required]
    [MinLength(1)]
    public string Reason { get; set; } = default!;
}