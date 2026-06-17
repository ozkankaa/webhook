using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace WebhookReceiver.Services;

public class WebhookEventDispatcher : IWebhookEventDispatcher
{
    private readonly IReadOnlyDictionary<string, IWebhookEventHandler> _handlers;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public WebhookEventDispatcher(IEnumerable<IWebhookEventHandler> handlers)
    {
        _handlers = handlers.ToDictionary(
            x => x.EventType,
            x => x,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task DispatchAsync(
        string eventType,
        string payloadJson,
        string deliveryId,
        CancellationToken cancellationToken)
    {
        if (!_handlers.TryGetValue(eventType, out var handler))
        {
            throw new InvalidOperationException(
                $"No webhook handler registered for event type '{eventType}'.");
        }

        var payload = JsonSerializer.Deserialize(
            payloadJson,
            handler.PayloadType,
            _jsonOptions);

        if (payload is null)
        {
            throw new WebhookPayloadValidationException(new[]
            {
                "Payload could not be deserialized."
            });
        }

        ValidatePayload(payload);

        await handler.HandleAsync(payload, deliveryId, cancellationToken);
    }

    private static void ValidatePayload(object payload)
    {
        var validationContext = new ValidationContext(payload);
        var validationResults = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(
            payload,
            validationContext,
            validationResults,
            validateAllProperties: true);

        if (!isValid)
        {
            throw new WebhookPayloadValidationException(
                validationResults.Select(x => x.ErrorMessage ?? "Invalid payload."));
        }
    }
}