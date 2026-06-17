using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebhookHub.Domain;
using WebhookHub.Dtos;
using WebhookHub.Filters;
using WebhookHub.Repositories;

namespace WebhookHub.Controllers;

/// <summary>
/// SubscriptionsController handles requests related to webhook subscriptions, including creating, updating, pausing, resuming, rotating secrets, and deactivating subscriptions. It provides endpoints for clients to manage their webhook subscriptions.
/// </summary>
[ApiController]
[Route("api/subscriptions")]
[Produces("application/json")]
[ApiKeyAuth]
public class SubscriptionsController : ControllerBase
{
    private readonly IWebhookSubscriptionRepository _repository;

    public SubscriptionsController(IWebhookSubscriptionRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Gets all webhook subscriptions. This endpoint retrieves a list of all existing webhook subscriptions, including their details such as event type, callback URL, active status, and creation timestamp.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of subscriptions with their details.</returns>
    [HttpGet]
    [EnableRateLimiting("read-deliveries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var subscriptions = await _repository.GetAllAsync(cancellationToken);

        return Ok(subscriptions.Select(x => new
        {
            x.Id,
            x.EventType,
            x.CallbackUrl,
            x.IsActive,
            x.CreatedAtUtc
        }));
    }

    /// <summary>
    /// Creates a new webhook subscription. This endpoint allows clients to create a new webhook subscription by providing the event type, callback URL, and secret. The subscription will be active upon creation.
    /// </summary>
    /// <param name="request">Subscription create request</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns created subscription details</returns>
    [HttpPost]
    [EnableRateLimiting("manage-subscriptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidHttpUrl(request.CallbackUrl))
        {
            ModelState.AddModelError(
                nameof(request.CallbackUrl),
                "CallbackUrl must be an absolute HTTP or HTTPS URL.");

            return ValidationProblem(ModelState);
        }

        var subscription = new WebhookSubscription
        {
            EventType = request.EventType,
            CallbackUrl = request.CallbackUrl,
            Secret = request.Secret,
            IsActive = true
        };

        await _repository.AddAsync(subscription, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = subscription.Id },
            new
            {
                subscription.Id,
                subscription.EventType,
                subscription.CallbackUrl,
                subscription.IsActive,
                subscription.CreatedAtUtc
            });
    }

    /// <summary>
    /// Gets a webhook subscription by its ID. This endpoint retrieves the details of a specific webhook subscription based on the provided subscription ID. If the subscription is found, it returns the subscription details; otherwise, it returns a 404 Not Found response.
    /// </summary>
    /// <param name="id">Subscription uniqfierid</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Returns requested subscripton details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting("read-deliveries")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(id, cancellationToken);

        if (subscription is null)
            return NotFound();

        return Ok(new
        {
            subscription.Id,
            subscription.EventType,
            subscription.CallbackUrl,
            subscription.IsActive,
            subscription.CreatedAtUtc
        });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting("manage-subscriptions")]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidHttpUrl(request.CallbackUrl))
        {
            ModelState.AddModelError(
                nameof(request.CallbackUrl),
                "CallbackUrl must be an absolute HTTP or HTTPS URL.");

            return ValidationProblem(ModelState);
        }

        var subscription = await _repository.GetByIdAsync(id, cancellationToken);

        if (subscription is null)
            return NotFound();

        subscription.EventType = request.EventType;
        subscription.CallbackUrl = request.CallbackUrl;

        await _repository.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            subscription.Id,
            subscription.EventType,
            subscription.CallbackUrl,
            subscription.IsActive,
            subscription.CreatedAtUtc
        });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting("manage-subscriptions")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(id, cancellationToken);

        if (subscription is null)
            return NotFound();

        subscription.IsActive = false;

        await _repository.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/pause")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting("manage-subscriptions")]
    public async Task<IActionResult> Pause(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(id, cancellationToken);

        if (subscription is null)
            return NotFound();

        subscription.IsActive = false;

        await _repository.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            subscription.Id,
            subscription.IsActive
        });
    }

    [HttpPost("{id:guid}/resume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting("manage-subscriptions")]
    public async Task<IActionResult> Resume(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(id, cancellationToken);

        if (subscription is null)
            return NotFound();

        subscription.IsActive = true;

        await _repository.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            subscription.Id,
            subscription.IsActive
        });
    }

    [HttpPost("{id:guid}/rotate-secret")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting("manage-subscriptions")]
    public async Task<IActionResult> RotateSecret(
        Guid id,
        RotateSubscriptionSecretRequest request,
        CancellationToken cancellationToken)
    {
        var subscription = await _repository.GetByIdAsync(id, cancellationToken);

        if (subscription is null)
            return NotFound();

        subscription.Secret = request.NewSecret;

        await _repository.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            subscription.Id,
            message = "Subscription secret rotated successfully."
        });
    }

    private static bool IsValidHttpUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && uri.Scheme is "http" or "https";
    }
}