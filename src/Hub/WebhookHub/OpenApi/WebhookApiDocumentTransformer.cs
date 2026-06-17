using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace WebhookHub.OpenApi;

public sealed class WebhookApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info.Title = "Webhook API";
        document.Info.Version = "v1";
        document.Info.Description = """
        Webhook API lets clients register webhook subscriptions and publish events.

        Protected endpoints require:

        X-Api-Key: your-api-key

        Main flow:
        1. Create a webhook subscription.
        2. Publish an event.
        3. Webhook API queues matching deliveries.
        4. Background worker sends signed HTTP POST callbacks.
        5. Failed deliveries are retried.
        6. Exhausted deliveries move to DeadLetter status.
        """;

        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes["ApiKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-Api-Key",
            Description = "API key required to access Webhook API management endpoints."
        };

        return Task.CompletedTask;
    }
}