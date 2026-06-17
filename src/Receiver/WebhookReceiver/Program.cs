using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using System.Text.Json;
using WebhookReceiver.Data;
using WebhookReceiver.Middleware;
using WebhookReceiver.OpenApi;
using WebhookReceiver.Options;
using WebhookReceiver.Services;

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
    .AddOptions<WebhookReceiverOptions>()
    .Bind(builder.Configuration.GetSection(WebhookReceiverOptions.SectionName))
    .Validate(options => options.EventSecrets.Count > 0,
        "At least one webhook event secret must be configured.")
    .Validate(options => options.EventSecrets.All(x => !string.IsNullOrWhiteSpace(x.Key)),
        "Event type cannot be empty.")
    .Validate(options => options.EventSecrets.All(x => !string.IsNullOrWhiteSpace(x.Value)),
        "Webhook secret cannot be empty.")
    .ValidateOnStart();

builder.Services.AddSingleton<IWebhookSignatureValidator, HmacSha256WebhookSignatureValidator>();
builder.Services.AddScoped<IProcessedDeliveryStore, SqliteProcessedDeliveryStore>();

builder.Services.AddDbContext<WebhookReceiverDbContext>(options =>
{
    options.UseSqlite(
    builder.Configuration.GetConnectionString("Default")); 
});

builder.Services.AddScoped<IProcessedDeliveryStore, SqliteProcessedDeliveryStore>();
builder.Services.AddScoped<IWebhookEventDispatcher, WebhookEventDispatcher>();
builder.Services.AddScoped<IWebhookEventHandler, OrderCreatedWebhookHandler>();
builder.Services.AddScoped<IWebhookEventHandler, OrderCancelledWebhookHandler>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<WebhookDocumentTransformer>();
    options.AddOperationTransformer<WebhookHeadersOperationTransformer>();
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<WebhookReceiverDbContext>(
        name: "webhookreceiver-database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" });

var app = builder.Build();


var skipDatabaseMigration = builder.Configuration
    .GetValue<bool>("SkipDatabaseMigration");

if (!skipDatabaseMigration)
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<WebhookReceiverDbContext>();

    dbContext.Database.Migrate();
}

app.UseMiddleware<CorrelationIdMiddleware>();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

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
        Log.Fatal(ex, "WebhookReceiver terminated unexpectedly.");
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