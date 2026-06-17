namespace WebhookHub.Services;

public interface IWebhookReplayService
{
    Task ReplayAsync(Guid deliveryId, CancellationToken cancellationToken);
}