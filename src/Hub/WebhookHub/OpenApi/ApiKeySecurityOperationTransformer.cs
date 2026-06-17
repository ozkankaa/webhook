using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace WebhookHub.OpenApi;

public sealed class ApiKeySecurityOperationTransformer : IOpenApiOperationTransformer
{
    private const string SchemeName = "ApiKey";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var path = context.Description.RelativePath;

        if (path is null)
            return Task.CompletedTask;

        var protectedPaths = new[]
        {
            "subscriptions",
            "events",
            "deliveries"
        };

        if (!protectedPaths.Any(x =>
                path.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = SchemeName
                }
            }] = []
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