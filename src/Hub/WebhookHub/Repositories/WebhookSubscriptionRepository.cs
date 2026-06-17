using Microsoft.EntityFrameworkCore;
using WebhookHub.Data;
using WebhookHub.Domain;

namespace WebhookHub.Repositories;

public class WebhookSubscriptionRepository : IWebhookSubscriptionRepository
{
    private readonly WebhookDbContext _dbContext;

    public WebhookSubscriptionRepository(WebhookDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WebhookSubscription subscription, CancellationToken cancellationToken)
    {
        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<WebhookSubscription>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Subscriptions
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<WebhookSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Subscriptions
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<List<WebhookSubscription>> GetActiveByEventTypeAsync(
        string eventType,
        CancellationToken cancellationToken)
    {
        return _dbContext.Subscriptions
            .Where(x => x.EventType == eventType && x.IsActive)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}