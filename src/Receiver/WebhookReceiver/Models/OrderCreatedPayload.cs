using System.ComponentModel.DataAnnotations;

namespace WebhookReceiver.Models;

/// <summary>
/// Payload for order.created events.
/// </summary>
public class OrderCreatedPayload
{
    /// <summary>
    /// Unique order identifier.
    /// </summary>
    /// <example>ORD-1001</example>
    [Required]
    [MinLength(1)]
    public string OrderId { get; set; } = default!;
    
    /// <summary>
    /// Unique customer identifier.
    /// </summary>
    /// <example>CUST-77</example>
    [Required]
    [MinLength(1)]
    public string CustomerId { get; set; } = default!;

    /// <summary>
    /// Total amount for the order.
    /// </summary>
    /// <example>149.99</example>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Total { get; set; }
}