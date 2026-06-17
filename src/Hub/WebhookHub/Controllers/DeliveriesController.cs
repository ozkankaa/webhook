using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WebhookHub.Data;
using WebhookHub.Domain;
using WebhookHub.Filters;
using WebhookHub.Services;

namespace WebhookHub.Controllers;

/// <summary>
/// DeliveriesController handles requests related to webhook deliveries, including retrieving delivery information, dead-lettered deliveries, and replaying failed deliveries. It provides endpoints for clients to access delivery data and manage delivery replays.
/// </summary>
[ApiController]
[Route("api/deliveries")]
[Produces("application/json")]
[ApiKeyAuth]
public class DeliveriesController : ControllerBase
{
    private readonly WebhookDbContext _dbContext;

    public DeliveriesController(WebhookDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Deliveries endpoint allows clients to retrieve information about webhook deliveries, including their status, attempts, and timestamps.
    /// </summary>
    /// <param name="dbContext">The database context used to access delivery data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of webhook deliveries with their details.</returns>
    [HttpPost("deliveries")]
    [EnableRateLimiting("read-deliveries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Deliveries(
        [FromServices] WebhookDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var deliveries = await dbContext.Deliveries
        .Include(x => x.Attempts)
        .OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new
        {
            x.Id,
            x.EventType,
            x.Status,
            x.AttemptCount,
            x.MaxAttempts,
            x.LastStatusCode,
            x.LastError,
            x.CreatedAtUtc,
            x.LastAttemptAtUtc,
            x.DeliveredAtUtc,
            x.DeadLetteredAtUtc,
            x.NextAttemptAtUtc,
            Attempts = x.Attempts
                .OrderBy(a => a.AttemptNumber)
                .Select(a => new
                {
                    a.AttemptNumber,
                    a.StatusCode,
                    a.IsSuccess,
                    a.Error,
                    a.AttemptedAtUtc
                })
        })
        .ToListAsync(cancellationToken);

        return Ok(deliveries);
    }

    /// <summary>
    /// Gets the list of webhook deliveries that have been marked as dead letters. Dead letters are deliveries that have failed after multiple attempts and are no longer retried.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of webhook deliveries with their details.</returns>
    [HttpGet("dead-letter")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDeadLetters(CancellationToken cancellationToken)
    {
        var deliveries = await _dbContext.Deliveries
            .Where(x => x.Status == WebhookDeliveryStatus.DeadLetter)
            .OrderByDescending(x => x.DeadLetteredAtUtc)
            .Select(x => new
            {
                x.Id,
                x.EventType,
                x.Status,
                x.AttemptCount,
                x.MaxAttempts,
                x.LastStatusCode,
                x.LastError,
                x.CreatedAtUtc,
                x.LastAttemptAtUtc,
                x.DeadLetteredAtUtc,
                x.NextAttemptAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(deliveries);
    }

    /// <summary>
    /// Attempts to replay a previously delivered webhook event with the specified delivery identifier.
    /// </summary>
    /// <remarks>Use this endpoint to request a replay of a specific webhook delivery. The operation is
    /// asynchronous; a successful response indicates that the replay request has been accepted, not that the replay has
    /// completed.</remarks>
    /// <param name="id">The unique identifier of the webhook delivery to replay.</param>
    /// <param name="replayService">The service used to perform the webhook replay operation.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the result of the replay operation. Returns 202 Accepted if the replay
    /// is accepted, 404 Not Found if the delivery is not found, or 400 Bad Request if the replay cannot be performed.</returns>
    [HttpPost("{id:guid}/replay")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Replay(
        Guid id,
        [FromServices] IWebhookReplayService replayService,
        CancellationToken cancellationToken)
    {
        try
        {
            await replayService.ReplayAsync(id, cancellationToken);
            return Accepted(new
            {
                deliveryId = id,
                message = "Delivery replay accepted."
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}