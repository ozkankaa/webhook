using Microsoft.AspNetCore.Mvc;

namespace WebhookHub.Filters;

public class ApiKeyAuthAttribute : TypeFilterAttribute
{
    public ApiKeyAuthAttribute()
        : base(typeof(ApiKeyAuthFilter))
    {
    }
}