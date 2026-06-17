using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using WebhookHub.Dtos;
using WebhookHub.Filters;
using WebhookHub.Services;

namespace WebhookHub.Controllers;

/// <summary>
/// Webhook handles incoming requests related to webhook subscriptions, event publishing, and delivery tracking. It provides endpoints for creating subscriptions, publishing events, and retrieving delivery information.
/// </summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
[ApiKeyAuth]
public class EventsController : ControllerBase
{
    /// <summary>
    /// Events endpoint allows clients to publish events to the webhook system. It accepts an event type and payload, and uses the provided publisher service to distribute the event to all relevant subscribers.
    /// </summary>
    /// <param name="request">The request containing the event details.</param>
    /// <param name="publisher">The service used to publish the event.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An accepted response indicating the event has been received for processing.</returns>
    [HttpPost]
    [EnableRateLimiting("publish-events")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Create(
        [FromBody] PublishEventRequest request,
        [FromServices] IWebhookPublisher publisher,
        CancellationToken cancellationToken)
    {
        if (request.Payload.ValueKind is not JsonValueKind.Object)
        {
            ModelState.AddModelError(
                nameof(request.Payload),
                "Payload must be a JSON object.");

            return ValidationProblem(ModelState);
        }

        await publisher.PublishAsync(
            request.EventType,
            request.Payload,
            cancellationToken);

        return Accepted();
    }
}
