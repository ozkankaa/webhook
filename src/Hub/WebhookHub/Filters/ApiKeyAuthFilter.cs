using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using WebhookHub.Options;

namespace WebhookHub.Filters;

public class ApiKeyAuthFilter : IAsyncAuthorizationFilter
{
    private readonly ApiKeyOptions _options;

    public ApiKeyAuthFilter(IOptions<ApiKeyOptions> options)
    {
        _options = options.Value;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(_options.HeaderName, out var providedApiKey))
        {
            context.Result = new UnauthorizedObjectResult("Missing API key.");
            return Task.CompletedTask;
        }

        if (!IsValidApiKey(providedApiKey.ToString(), _options.Key))
        {
            context.Result = new UnauthorizedObjectResult("Invalid API key.");
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private static bool IsValidApiKey(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return providedBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}