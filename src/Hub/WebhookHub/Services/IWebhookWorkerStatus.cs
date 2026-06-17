namespace WebhookHub.Services;

public interface IWebhookWorkerStatus
{
    DateTime? LastHeartbeatUtc { get; }
    void Beat();
}