namespace WebhookReceiver.Services;

public class WebhookPayloadValidationException : Exception
{
    public WebhookPayloadValidationException(IEnumerable<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors.ToList();
    }

    public IReadOnlyList<string> Errors { get; }
}