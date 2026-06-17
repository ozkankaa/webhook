using WebhookHub.Domain;

namespace WebhookHub.Repositories;

public interface IWebhookDeliveryRepository
{
    Task AddRangeAsync(IEnumerable<WebhookDelivery> deliveries, CancellationToken cancellationToken);
    Task<List<WebhookDelivery>> GetPendingDeliveriesAsync(int take, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}