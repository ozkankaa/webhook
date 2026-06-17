using WebhookReceiver.Models;
using WebhookReceiver.Services;

namespace WebhookReceiver.Tests;

public class WebhookEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_RoutesToCorrectHandler()
    {
        var handler = new FakeOrderCreatedHandler();

        var dispatcher = new WebhookEventDispatcher(
        [
            handler
        ]);

        await dispatcher.DispatchAsync(
            "order.created",
            """
            {
              "orderId": "ORD-1",
              "customerId": "CUST-1",
              "total": 25.50
            }
            """,
            "delivery-1",
            CancellationToken.None);

        Assert.True(handler.WasCalled);
        Assert.Equal("ORD-1", handler.Payload?.OrderId);
    }

    [Fact]
    public async Task DispatchAsync_ThrowsValidationException_WhenPayloadInvalid()
    {
        var dispatcher = new WebhookEventDispatcher(
        [
            new FakeOrderCreatedHandler()
        ]);

        await Assert.ThrowsAsync<WebhookPayloadValidationException>(() =>
            dispatcher.DispatchAsync(
                "order.created",
                """
                {
                  "orderId": "",
                  "customerId": "",
                  "total": 0
                }
                """,
                "delivery-1",
                CancellationToken.None));
    }

    private sealed class FakeOrderCreatedHandler
        : WebhookEventHandler<OrderCreatedPayload>
    {
        public override string EventType => "order.created";

        public bool WasCalled { get; private set; }
        public OrderCreatedPayload? Payload { get; private set; }

        protected override Task HandleAsync(
            OrderCreatedPayload payload,
            string deliveryId,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            Payload = payload;
            return Task.CompletedTask;
        }
    }
}