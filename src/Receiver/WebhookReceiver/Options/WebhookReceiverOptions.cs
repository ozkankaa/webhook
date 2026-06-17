namespace WebhookReceiver.Options;

public class WebhookReceiverOptions
{
    public const string SectionName = "WebhookReceiver";

    public Dictionary<string, string> EventSecrets { get; set; } = new();
}