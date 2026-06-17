using System.Text;
using WebhookHub.Domain;

namespace WebhookHub.Services;

public class WebhookDeliveryClient : IWebhookDeliveryClient
{
    private readonly HttpClient _httpClient;
    private readonly IWebhookSignatureService _signatureService;
    private readonly IWebhookRetryPolicy _retryPolicy;
    private readonly ILogger<WebhookDeliveryClient> _logger;

    public WebhookDeliveryClient(
        HttpClient httpClient,
        IWebhookSignatureService signatureService,
        IWebhookRetryPolicy retryPolicy,
        ILogger<WebhookDeliveryClient> logger)
    {
        _httpClient = httpClient;
        _signatureService = signatureService;
        _retryPolicy = retryPolicy;
        _logger = logger;
    }

    public async Task DeliverAsync(
        WebhookDelivery delivery,
        CancellationToken cancellationToken)
    {
        delivery.AttemptCount++;
        delivery.LastAttemptAtUtc = DateTime.UtcNow;
        var correlationId = delivery.Id.ToString();

        try
        {
            var signature = _signatureService.Sign(
                delivery.PayloadJson,
                delivery.Subscription.Secret);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                delivery.Subscription.CallbackUrl);

            request.Content = new StringContent(
                delivery.PayloadJson,
                Encoding.UTF8,
                "application/json");

            request.Headers.Add("X-Webhook-Event", delivery.EventType);
            request.Headers.Add("X-Webhook-Signature", signature);
            request.Headers.Add("X-Webhook-Delivery-Id", delivery.Id.ToString());
            request.Headers.Add("X-Correlation-Id", correlationId);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            delivery.LastStatusCode = (int)response.StatusCode;

            AddAttempt(
                delivery,
                statusCode: (int)response.StatusCode,
                isSuccess: response.IsSuccessStatusCode,
                error: response.IsSuccessStatusCode
                    ? null
                    : $"HTTP {(int)response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Webhook delivery succeeded. EventType: {EventType}, DeliveryId: {DeliveryId}, CorrelationId: {CorrelationId}",
                    delivery.EventType,
                    delivery.Id,
                    correlationId);

                MarkSuccess(delivery);
                return;
            }

            MarkRetryOrDeadLetter(
                delivery,
                $"HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Webhook delivery failed. DeliveryId: {DeliveryId}",
                delivery.Id);

            AddAttempt(
                delivery,
                statusCode: null,
                isSuccess: false,
                error: ex.Message);

            MarkRetryOrDeadLetter(delivery, ex.Message);
        }
    }

    private static void MarkSuccess(WebhookDelivery delivery)
    {
        delivery.Status = WebhookDeliveryStatus.Success;
        delivery.DeliveredAtUtc = DateTime.UtcNow;
        delivery.LastError = null;
    }

    private void MarkRetryOrDeadLetter(
        WebhookDelivery delivery,
        string error)
    {
        delivery.LastError = error;

        if (delivery.AttemptCount >= delivery.MaxAttempts)
        {
            delivery.Status = WebhookDeliveryStatus.DeadLetter;
            delivery.DeadLetteredAtUtc = DateTime.UtcNow;
            return;
        }

        delivery.Status = WebhookDeliveryStatus.Retrying;
        delivery.NextAttemptAtUtc =
            _retryPolicy.GetNextAttemptUtc(delivery.AttemptCount);
    }

    private static void AddAttempt(
        WebhookDelivery delivery,
        int? statusCode,
        bool isSuccess,
        string? error)
    {
        delivery.Attempts.Add(new WebhookDeliveryAttempt
        {
            DeliveryId = delivery.Id,
            AttemptNumber = delivery.AttemptCount,
            StatusCode = statusCode,
            IsSuccess = isSuccess,
            Error = error,
            AttemptedAtUtc = DateTime.UtcNow
        });
    }
}