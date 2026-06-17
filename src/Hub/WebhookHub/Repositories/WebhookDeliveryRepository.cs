using Microsoft.EntityFrameworkCore;
using WebhookHub.Data;
using WebhookHub.Domain;

namespace WebhookHub.Repositories;

public class WebhookDeliveryRepository : IWebhookDeliveryRepository
{
    private readonly WebhookDbContext _dbContext;

    public WebhookDeliveryRepository(WebhookDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddRangeAsync(IEnumerable<WebhookDelivery> deliveries, CancellationToken cancellationToken)
    {
        _dbContext.Deliveries.AddRange(deliveries);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<WebhookDelivery>> GetPendingDeliveriesAsync(int take, CancellationToken cancellationToken)
    {
        return _dbContext.Deliveries
                .Include(x => x.Subscription)
                .Where(x =>
                    (x.Status == WebhookDeliveryStatus.Pending ||
                     x.Status == WebhookDeliveryStatus.Retrying) &&
                    x.NextAttemptAtUtc <= DateTime.UtcNow &&
                    x.AttemptCount < x.MaxAttempts)
                .OrderBy(x => x.NextAttemptAtUtc)
                .Take(take)
                .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}