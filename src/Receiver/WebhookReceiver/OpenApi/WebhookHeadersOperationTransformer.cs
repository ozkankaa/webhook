using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace WebhookReceiver.OpenApi;

public sealed class WebhookHeadersOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var path = context.Description.RelativePath;

        if (path is null || !path.StartsWith("webhooks/", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        operation.Parameters ??= [];

        operation.Parameters.Add(CreateHeader(
            "X-Webhook-Event",
            required: true,
            description: "Webhook event type, for example order.created."));

        operation.Parameters.Add(CreateHeader(
            "X-Webhook-Delivery-Id",
            required: true,
            description: "Unique delivery ID used for idempotency."));

        operation.Parameters.Add(CreateHeader(
            "X-Webhook-Signature",
            required: true,
            description: "HMAC-SHA256 signature of the raw request body."));

        operation.Parameters.Add(CreateHeader(
            "X-Correlation-Id",
            required: false,
            description: "Correlation ID used for tracing logs across sender and receiver."));

        operation.Summary ??= "Receives a signed webhook event.";
        operation.Description ??= """
        Validates webhook headers, verifies the HMAC signature, checks idempotency,
        dispatches the event to the correct handler, and stores the processed delivery ID.
        """;

        return Task.CompletedTask;
    }

    private static OpenApiParameter CreateHeader(
        string name,
        bool required,
        string description)
    {
        return new OpenApiParameter
        {
            Name = name,
            In = ParameterLocation.Header,
            Required = required,
            Description = description,
            Schema = new OpenApiSchema
            {
                Type = "string"
            }
        };
    }
}