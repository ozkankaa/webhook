using WebhookHub.Repositories;
using WebhookHub.Services;

namespace WebhookHub.Background;

public class WebhookDeliveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWebhookWorkerStatus _workerStatus;
    private readonly ILogger<WebhookDeliveryWorker> _logger;

    public WebhookDeliveryWorker(
        IServiceScopeFactory scopeFactory,
        IWebhookWorkerStatus workerStatus,
        ILogger<WebhookDeliveryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _workerStatus = workerStatus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Webhook delivery worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _workerStatus.Beat();

            using var scope = _scopeFactory.CreateScope();

            var deliveryRepository = scope.ServiceProvider
                .GetRequiredService<IWebhookDeliveryRepository>();

            var deliveryClient = scope.ServiceProvider
                .GetRequiredService<IWebhookDeliveryClient>();

            var deliveries = await deliveryRepository
                .GetPendingDeliveriesAsync(10, stoppingToken);

            foreach (var delivery in deliveries)
            {
                await deliveryClient.DeliverAsync(delivery, stoppingToken);
            }

            await deliveryRepository.SaveChangesAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}