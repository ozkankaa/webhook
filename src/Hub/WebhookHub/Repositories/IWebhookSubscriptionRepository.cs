using WebhookHub.Domain;

namespace WebhookHub.Repositories;

public interface IWebhookSubscriptionRepository
{
    Task AddAsync(WebhookSubscription subscription, CancellationToken cancellationToken);
    Task<List<WebhookSubscription>> GetAllAsync(CancellationToken cancellationToken);
    Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<WebhookSubscription>> GetActiveByEventTypeAsync(string eventType, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}