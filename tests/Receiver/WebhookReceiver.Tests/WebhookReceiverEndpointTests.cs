using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebhookReceiver.Data;

namespace WebhookReceiver.Tests;

public class WebhookReceiverEndpointTests : IClassFixture<WebhookReceiverApiFactory>
{
    private readonly WebhookReceiverApiFactory _factory;
    private readonly HttpClient _client;

    public WebhookReceiverEndpointTests(WebhookReceiverApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ReceiveWebhook_ReturnsOk_WhenSignatureIsValid()
    {
        var payload = """
        {
          "orderId": "ORD-100",
          "customerId": "CUST-100",
          "total": 49.99
        }
        """;

        var request = CreateWebhookRequest(
            eventType: "order.created",
            deliveryId: Guid.NewGuid().ToString(),
            payloadJson: payload,
            secret: "super-secret");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReceiveWebhook_ReturnsUnauthorized_WhenSignatureIsInvalid()
    {
        var payload = """
        {
          "orderId": "ORD-100",
          "customerId": "CUST-100",
          "total": 49.99
        }
        """;

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/webhooks/orders");

        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Webhook-Event", "order.created");
        request.Headers.Add("X-Webhook-Delivery-Id", Guid.NewGuid().ToString());
        request.Headers.Add("X-Webhook-Signature", "bad-signature");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReceiveWebhook_ReturnsBadRequest_WhenPayloadIsInvalid()
    {
        var payload = """
        {
          "orderId": "",
          "customerId": "",
          "total": 0
        }
        """;

        var request = CreateWebhookRequest(
            eventType: "order.created",
            deliveryId: Guid.NewGuid().ToString(),
            payloadJson: payload,
            secret: "super-secret");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ReceiveWebhook_IsIdempotent_ForDuplicateDeliveryId()
    {
        var deliveryId = Guid.NewGuid().ToString();

        var payload = """
        {
          "orderId": "ORD-200",
          "customerId": "CUST-200",
          "total": 99.99
        }
        """;

        var firstRequest = CreateWebhookRequest(
            eventType: "order.created",
            deliveryId: deliveryId,
            payloadJson: payload,
            secret: "super-secret");

        var secondRequest = CreateWebhookRequest(
            eventType: "order.created",
            deliveryId: deliveryId,
            payloadJson: payload,
            secret: "super-secret");

        var firstResponse = await _client.SendAsync(firstRequest);
        var secondResponse = await _client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WebhookReceiverDbContext>();

        var count = await dbContext.ProcessedDeliveries
            .CountAsync(x => x.DeliveryId == deliveryId);

        Assert.Equal(1, count);
    }

    private static HttpRequestMessage CreateWebhookRequest(
        string eventType,
        string deliveryId,
        string payloadJson,
        string secret)
    {
        var signature = WebhookTestSignature.Sign(payloadJson, secret);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/webhooks/orders");

        request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Webhook-Event", eventType);
        request.Headers.Add("X-Webhook-Delivery-Id", deliveryId);
        request.Headers.Add("X-Webhook-Signature", signature);

        return request;
    }
}