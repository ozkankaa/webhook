using System.ComponentModel.DataAnnotations;

namespace WebhookHub.Dtos;

public class RotateSubscriptionSecretRequest
{
    [Required]
    [MinLength(16)]
    [MaxLength(200)]
    public string NewSecret { get; set; } = default!;
}