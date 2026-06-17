using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json;
using System.Threading.RateLimiting;
using WebhookHub.Background;
using WebhookHub.Data;
using WebhookHub.Filters;
using WebhookHub.HealthChecks;
using WebhookHub.OpenApi;
using WebhookHub.Repositories;
using WebhookHub.Services;
using ApiKeyOptions = WebhookHub.Options.ApiKeyOptions;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

if (environment != "Testing")
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(
            new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build())
        .CreateBootstrapLogger();
}

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.EnvironmentName != "Testing")
{
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services);
    });
}

// Add services to the container.

builder.Services
    .AddOptions<ApiKeyOptions>()
    .Bind(builder.Configuration.GetSection(ApiKeyOptions.SectionName))
    .Validate(x => !string.IsNullOrWhiteSpace(x.HeaderName), "API key header name is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Key), "API key is required.")
    .ValidateOnStart();

builder.Services.AddScoped<ApiKeyAuthFilter>();

builder.Services.AddDbContext<WebhookDbContext>(options =>
{
    options.UseSqlite(
    builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddScoped<IWebhookSubscriptionRepository, WebhookSubscriptionRepository>();
builder.Services.AddScoped<IWebhookDeliveryRepository, WebhookDeliveryRepository>();

builder.Services.AddScoped<IWebhookPublisher, WebhookPublisher>();
builder.Services.AddScoped<IWebhookSignatureService, HmacSha256WebhookSignatureService>();
builder.Services.AddScoped<IWebhookRetryPolicy, ExponentialBackoffRetryPolicy>();

builder.Services.AddHttpClient<IWebhookDeliveryClient, WebhookDeliveryClient>();

builder.Services.AddHostedService<WebhookDeliveryWorker>();
builder.Services.AddSingleton<IWebhookWorkerStatus, WebhookWorkerStatus>();
builder.Services.AddScoped<IWebhookReplayService, WebhookReplayService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<WebhookApiDocumentTransformer>();
    options.AddOperationTransformer<WebhookApiOperationTransformer>();
    options.AddOperationTransformer<ApiKeySecurityOperationTransformer>();
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => x.Key,
                x => x.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        return new BadRequestObjectResult(new
        {
            message = "Request validation failed.",
            errors
        });
    };
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<WebhookDbContext>(
        name: "webhookhub-database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" })
    .AddCheck<WebhookWorkerHealthCheck>(
        name: "webhook-delivery-worker",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            message = "Rate limit exceeded. Please retry later."
        }, cancellationToken);
    };

    options.AddPolicy("publish-events", httpContext =>
    {
        var apiKey = httpContext.Request.Headers["X-Api-Key"].FirstOrDefault()
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: apiKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("manage-subscriptions", httpContext =>
    {
        var apiKey = httpContext.Request.Headers["X-Api-Key"].FirstOrDefault()
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: apiKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.AddPolicy("read-deliveries", httpContext =>
    {
        var apiKey = httpContext.Request.Headers["X-Api-Key"].FirstOrDefault()
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: apiKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("CorrelationId",
                httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                ?? httpContext.TraceIdentifier);

            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        };
    });
}

var skipDatabaseMigration = builder.Configuration
    .GetValue<bool>("SkipDatabaseMigration");

if (!skipDatabaseMigration)
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<WebhookDbContext>();

    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
});

if (app.Environment.IsEnvironment("Testing"))
{
    app.Run();
}
else
{
    try
    {
        app.Run();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "WebhookHub terminated unexpectedly.");
        throw;
    }
    finally
    {
        Log.CloseAndFlush();
    }
}

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(x => new
        {
            name = x.Key,
            status = x.Value.Status.ToString(),
            description = x.Value.Description,
            duration = x.Value.Duration.TotalMilliseconds
        })
    };

    return context.Response.WriteAsync(JsonSerializer.Serialize(response));
}

public partial class Program { }