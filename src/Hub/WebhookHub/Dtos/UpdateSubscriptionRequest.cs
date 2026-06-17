using System.ComponentModel.DataAnnotations;

namespace WebhookHub.Dtos;

public class UpdateSubscriptionRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string EventType { get; set; } = default!;

    [Required]
    [Url]
    [MaxLength(500)]
    public string CallbackUrl { get; set; } = default!;
}