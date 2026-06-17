using System.Net;
using System.Net.Http.Json;

namespace WebhookHub.Tests;

public class WebhookApiRateLimitTests : IClassFixture<WebhookHubApiFactory>
{
    [Fact]
    public async Task PublishEvent_ReturnsTooManyRequests_WhenRateLimitExceeded()
    {
        await using var factory = new WebhookHubApiFactory();
        using var client = factory.CreateClient();
        HttpResponseMessage? lastResponse = null;

        for (var i = 0; i < 25; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/events");

            request.Headers.Add("X-Api-Key", "test-api-key");
            request.Content = JsonContent.Create(new
            {
                eventType = "order.created",
                payload = new
                {
                    orderId = $"ORD-{i}",
                    customerId = "CUST-1",
                    total = 10.0
                }
            });

            lastResponse = await client.SendAsync(request);
        }

        Assert.NotNull(lastResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse!.StatusCode);
    }
}