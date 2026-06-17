namespace WebhookHub.Services;

public interface IWebhookRetryPolicy
{
    DateTime GetNextAttemptUtc(int attemptCount);
}