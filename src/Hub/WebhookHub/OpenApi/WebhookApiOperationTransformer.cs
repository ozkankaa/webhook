using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace WebhookHub.OpenApi;

public sealed class WebhookApiOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var path = context.Description.RelativePath;

        if (path is null)
            return Task.CompletedTask;

        if (path.Equals("api/subscriptions", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "List or create webhook subscriptions.";
            operation.Description = "Lists existing webhook subscriptions or creates a new subscription.";
        }

        if (path.StartsWith("api/subscriptions/{id}", StringComparison.OrdinalIgnoreCase))
        {
            operation.Description = """
            Manages a webhook subscription.

            Supported operations:
            - update callback URL and event type
            - pause delivery
            - resume delivery
            - rotate secret
            - deactivate subscription
            """;
        }

        if (path.Equals("api/deliveries/dead-letter", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "List dead-letter deliveries.";
            operation.Description = "Returns webhook deliveries that exhausted all retry attempts.";
        }

        if (path.Equals("api/deliveries/{id}/replay", StringComparison.OrdinalIgnoreCase))
        {
            operation.Summary = "Replay dead-letter delivery.";
            operation.Description = """
            Resets a dead-letter delivery back to Pending.

            The background worker will pick it up and attempt delivery again.
            """;

            operation.Responses.TryAdd("202", new OpenApiResponse
            {
                Description = "Replay accepted."
            });
        }

        operation.Responses.TryAdd("400", new OpenApiResponse
        {
            Description = "Request validation failed."
        });

        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Missing or invalid API key."
        });

        operation.Responses.TryAdd("429", new OpenApiResponse
        {
            Description = "Rate limit exceeded."
        });

        return Task.CompletedTask;
    }
}