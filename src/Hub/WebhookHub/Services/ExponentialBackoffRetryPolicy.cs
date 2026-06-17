namespace WebhookHub.Services;

public class ExponentialBackoffRetryPolicy : IWebhookRetryPolicy
{
    public DateTime GetNextAttemptUtc(int attemptCount)
    {
        var delaySeconds = attemptCount switch
        {
            1 => 10,
            2 => 30,
            3 => 60,
            4 => 120,
            _ => 300
        };

        return DateTime.UtcNow.AddSeconds(delaySeconds);
    }
}