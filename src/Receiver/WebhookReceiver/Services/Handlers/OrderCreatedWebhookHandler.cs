using WebhookReceiver.Models;

namespace WebhookReceiver.Services;

public class OrderCreatedWebhookHandler : WebhookEventHandler<OrderCreatedPayload>
{
    private readonly ILogger<OrderCreatedWebhookHandler> _logger;

    public OrderCreatedWebhookHandler(ILogger<OrderCreatedWebhookHandler> logger)
    {
        _logger = logger;
    }

    public override string EventType => "order.created";

    protected override Task HandleAsync(
        OrderCreatedPayload payload,
        string deliveryId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Order created handled. OrderId: {OrderId}, CustomerId: {CustomerId}, Total: {Total}, DeliveryId: {DeliveryId}",
            payload.OrderId,
            payload.CustomerId,
            payload.Total,
            deliveryId);

        return Task.CompletedTask;
    }
}