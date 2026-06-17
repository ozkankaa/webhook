using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace WebhookReceiver.OpenApi;

public sealed class WebhookDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title = "WebhookReceiver API";
        document.Info.Version = "v1";
        document.Info.Description = """
        Receives signed webhook events from WebhookHub.

        Signature:
        HMAC-SHA256 over the raw JSON request body.

        Idempotency:
        X-Webhook-Delivery-Id is used as the idempotency key.
        Duplicate delivery IDs are ignored after successful processing.
        """;

        return Task.CompletedTask;
    }
}