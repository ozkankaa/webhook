namespace WebhookReceiver.Services;

public abstract class WebhookEventHandler<TPayload> : IWebhookEventHandler
    where TPayload : class
{
    public abstract string EventType { get; }

    public Type PayloadType => typeof(TPayload);

    public Task HandleAsync(
        object payload,
        string deliveryId,
        CancellationToken cancellationToken)
    {
        return HandleAsync((TPayload)payload, deliveryId, cancellationToken);
    }

    protected abstract Task HandleAsync(
        TPayload payload,
        string deliveryId,
        CancellationToken cancellationToken);
}