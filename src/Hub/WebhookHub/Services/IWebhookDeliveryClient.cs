using WebhookHub.Domain;

namespace WebhookHub.Services;

public interface IWebhookDeliveryClient
{
    Task DeliverAsync(WebhookDelivery delivery, CancellationToken cancellationToken);
}