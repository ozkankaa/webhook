using WebhookReceiver.Models;

namespace WebhookReceiver.Services;

public class OrderCancelledWebhookHandler : WebhookEventHandler<OrderCancelledPayload>
{
    private readonly ILogger<OrderCancelledWebhookHandler> _logger;

    public OrderCancelledWebhookHandler(ILogger<OrderCancelledWebhookHandler> logger)
    {
        _logger = logger;
    }

    public override string EventType => "order.cancelled";

    protected override Task HandleAsync(
        OrderCancelledPayload payload,
        string deliveryId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Order cancelled handled. OrderId: {OrderId}, Reason: {Reason}, DeliveryId: {DeliveryId}",
            payload.OrderId,
            payload.Reason,
            deliveryId);

        return Task.CompletedTask;
    }
}