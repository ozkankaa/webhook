using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WebhookReceiver.Data;
using WebhookReceiver.Domain;

namespace WebhookReceiver.Services;

public class SqliteProcessedDeliveryStore : IProcessedDeliveryStore
{
    private readonly WebhookReceiverDbContext _dbContext;

    public SqliteProcessedDeliveryStore(WebhookReceiverDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> HasProcessedAsync(
        string deliveryId,
        CancellationToken cancellationToken)
    {
        return _dbContext.ProcessedDeliveries
            .AnyAsync(x => x.DeliveryId == deliveryId, cancellationToken);
    }

    public async Task MarkProcessedAsync(
        string deliveryId,
        string eventType,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var record = new ProcessedWebhookDelivery
        {
            DeliveryId = deliveryId,
            EventType = eventType,
            PayloadHash = ComputeSha256(payloadJson),
            ProcessedAtUtc = DateTime.UtcNow
        };

        _dbContext.ProcessedDeliveries.Add(record);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Unique index protects us from race-condition duplicates.
            // If another request inserted the same DeliveryId first,
            // this request should be treated as already processed.
        }
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}