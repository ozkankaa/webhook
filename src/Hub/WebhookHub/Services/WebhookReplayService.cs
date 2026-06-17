using Microsoft.EntityFrameworkCore;
using WebhookHub.Data;
using WebhookHub.Domain;

namespace WebhookHub.Services;

public class WebhookReplayService : IWebhookReplayService
{
    private readonly WebhookDbContext _dbContext;

    public WebhookReplayService(WebhookDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ReplayAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        var delivery = await _dbContext.Deliveries
            .FirstOrDefaultAsync(x => x.Id == deliveryId, cancellationToken);

        if (delivery is null)
            throw new KeyNotFoundException("Delivery not found.");

        if (delivery.Status != WebhookDeliveryStatus.DeadLetter)
            throw new InvalidOperationException("Only dead-letter deliveries can be replayed.");

        delivery.Status = WebhookDeliveryStatus.Pending;
        delivery.AttemptCount = 0;
        delivery.LastStatusCode = null;
        delivery.LastError = null;
        delivery.DeadLetteredAtUtc = null;
        delivery.DeliveredAtUtc = null;
        delivery.NextAttemptAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}