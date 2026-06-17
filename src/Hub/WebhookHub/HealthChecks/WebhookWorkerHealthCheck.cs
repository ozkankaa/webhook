using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebhookHub.Services;

namespace WebhookHub.HealthChecks;

public class WebhookWorkerHealthCheck : IHealthCheck
{
    private readonly IWebhookWorkerStatus _workerStatus;

    public WebhookWorkerHealthCheck(IWebhookWorkerStatus workerStatus)
    {
        _workerStatus = workerStatus;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var lastHeartbeat = _workerStatus.LastHeartbeatUtc;

        if (lastHeartbeat is null)
            return Task.FromResult(HealthCheckResult.Unhealthy("Worker has not started."));

        var age = DateTime.UtcNow - lastHeartbeat.Value;

        if (age > TimeSpan.FromSeconds(30))
            return Task.FromResult(HealthCheckResult.Unhealthy($"Worker heartbeat is stale: {age.TotalSeconds:N0}s."));

        return Task.FromResult(HealthCheckResult.Healthy("Worker is running."));
    }
}