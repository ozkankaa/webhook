using System.Text.Json;

namespace WebhookHub.Services;

public interface IWebhookPublisher
{
    Task PublishAsync(string eventType, JsonElement payload, CancellationToken cancellationToken);
}