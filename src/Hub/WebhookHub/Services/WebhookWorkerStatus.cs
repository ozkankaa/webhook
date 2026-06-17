namespace WebhookHub.Services;

public class WebhookWorkerStatus : IWebhookWorkerStatus
{
    private DateTime? _lastHeartbeatUtc;

    public DateTime? LastHeartbeatUtc => _lastHeartbeatUtc;

    public void Beat()
    {
        _lastHeartbeatUtc = DateTime.UtcNow;
    }
}