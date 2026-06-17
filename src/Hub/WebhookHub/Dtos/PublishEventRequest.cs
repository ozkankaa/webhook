using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace WebhookHub.Dtos;
/// <summary>
/// Provides the necessary information to publish an event, including the event type and the payload to be sent to subscribers.
/// </summary>
/// <param name="EventType">The type of event being published.</param>
/// <param name="Payload">The payload containing the event data.</param>
public record PublishEventRequest(
    /// <summary>
    /// The type of event being published.
    /// </summary>
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    string EventType,
    /// <summary>
    /// The payload containing the event data.
    /// </summary>
    [Required]
    JsonElement Payload
);