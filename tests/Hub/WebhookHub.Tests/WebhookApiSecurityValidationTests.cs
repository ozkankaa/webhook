using System.Net;
using System.Net.Http.Json;

namespace WebhookHub.Tests;

public class WebhookApiSecurityValidationTests : IClassFixture<WebhookHubApiFactory>
{
    private readonly HttpClient _client;

    public WebhookApiSecurityValidationTests(WebhookHubApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateSubscription_ReturnsUnauthorized_WhenApiKeyMissing()
    {
        var response = await _client.PostAsJsonAsync("api/subscriptions", new
        {
            eventType = "order.created",
            callbackUrl = "http://localhost/webhooks/orders",
            secret = "super-secret-value"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSubscription_ReturnsUnauthorized_WhenApiKeyInvalid()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/subscriptions");

        request.Headers.Add("X-Api-Key", "wrong-api-key");
        request.Content = JsonContent.Create(new
        {
            eventType = "order.created",
            callbackUrl = "http://localhost/webhooks/orders",
            secret = "super-secret-value"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateSubscription_ReturnsBadRequest_WhenRequestInvalid()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/subscriptions");

        request.Headers.Add("X-Api-Key", "test-api-key");
        request.Content = JsonContent.Create(new
        {
            eventType = "",
            callbackUrl = "not-a-valid-url",
            secret = "short"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateSubscription_ReturnsCreated_WhenRequestValid()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/subscriptions");

        request.Headers.Add("X-Api-Key", "test-api-key");
        request.Content = JsonContent.Create(new
        {
            eventType = "order.created",
            callbackUrl = "http://localhost/webhooks/orders",
            secret = "super-secret-value"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PublishEvent_ReturnsBadRequest_WhenPayloadIsNotObject()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/events");

        request.Headers.Add("X-Api-Key", "test-api-key");
        request.Content = JsonContent.Create(new
        {
            eventType = "order.created",
            payload = "invalid-payload"
        });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}