using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WebhookReceiver.Options;
using WebhookReceiver.Services;

namespace WebhookReceiver.Controllers
{
    [ApiController]
    [Route("webhooks")]
    [Produces("application/json")]
    public class WebhooksController : ControllerBase
    {
        private readonly IWebhookSignatureValidator _signatureValidator;
        private readonly IProcessedDeliveryStore _processedDeliveryStore;
        private readonly IOptions<WebhookReceiverOptions> _options;
        private readonly IWebhookEventDispatcher _dispatcher;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            IWebhookSignatureValidator signatureValidator,
            IProcessedDeliveryStore processedDeliveryStore,
            IOptions<WebhookReceiverOptions> options,
            IWebhookEventDispatcher dispatcher,
            ILogger<WebhooksController> logger)
        {
            _signatureValidator = signatureValidator;
            _processedDeliveryStore = processedDeliveryStore;
            _options = options;
            _dispatcher = dispatcher;
            _logger = logger;
        }

        /// <summary>
        /// Receives order webhook events.
        /// </summary>
        /// <remarks>
        /// Example request:
        ///
        /// POST /webhooks/orders
        ///
        /// Required headers:
        ///
        /// - X-Webhook-Event
        /// - X-Webhook-Delivery-Id
        /// - X-Webhook-Signature
        /// - X-Correlation-Id
        ///
        /// Example payload for order.created:
        ///
        /// {
        ///   "orderId": "ORD-1001",
        ///   "customerId": "CUST-77",
        ///   "total": 149.99
        /// }
        /// </remarks>
        /// <response code="200">Webhook accepted.</response>
        /// <response code="400">Payload validation failed.</response>
        /// <response code="401">Signature validation failed.</response>
        [HttpPost("orders")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ReceiveOrderWebhook(CancellationToken cancellationToken)
        {
            if (!Request.Headers.TryGetValue("X-Webhook-Signature", out var signature))
                return Unauthorized();

            if (!Request.Headers.TryGetValue("X-Webhook-Delivery-Id", out var deliveryId))
                return BadRequest("Missing X-Webhook-Delivery-Id header.");

            if (!Request.Headers.TryGetValue("X-Webhook-Event", out var eventType))
                return BadRequest("Missing X-Webhook-Event header.");

            var eventTypeValue = eventType.ToString();
            var deliveryIdValue = deliveryId.ToString();

            _logger.LogInformation(
                "Webhook accepted. EventType: {EventType}, DeliveryId: {DeliveryId}",
                eventTypeValue,
                deliveryIdValue);

            if (!_options.Value.EventSecrets.TryGetValue(eventTypeValue, out var secret))
                return BadRequest($"Unsupported event type: {eventTypeValue}");

            using var reader = new StreamReader(Request.Body);
            var payloadJson = await reader.ReadToEndAsync(cancellationToken);

            var isValid = _signatureValidator.IsValid(
                payloadJson,
                secret,
                signature.ToString());

            if (!isValid)
                return Unauthorized();

            if (await _processedDeliveryStore.HasProcessedAsync(deliveryIdValue, cancellationToken))
            {
                return Ok(new { message = "Duplicate delivery ignored." });
            }

            using var document = JsonDocument.Parse(payloadJson);

            try
            {
                await _dispatcher.DispatchAsync(
                    eventTypeValue,
                    payloadJson,
                    deliveryIdValue,
                    cancellationToken);
            }
            catch (WebhookPayloadValidationException ex)
            {
                return BadRequest(new
                {
                    message = "Invalid webhook payload.",
                    errors = ex.Errors
                });
            }

            await _processedDeliveryStore.MarkProcessedAsync(
                deliveryIdValue,
                eventTypeValue,
                payloadJson,
                cancellationToken);

            return Ok(new
            {
                message = "Webhook processed successfully.",
                eventType = eventTypeValue,
                deliveryId = deliveryIdValue
            });
        }
    }
}
