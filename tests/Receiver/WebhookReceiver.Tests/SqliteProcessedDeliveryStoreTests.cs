using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WebhookReceiver.Data;
using WebhookReceiver.Services;

namespace WebhookReceiver.Tests;

public class SqliteProcessedDeliveryStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebhookReceiverDbContext _dbContext;
    private readonly SqliteProcessedDeliveryStore _store;

    public SqliteProcessedDeliveryStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<WebhookReceiverDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new WebhookReceiverDbContext(options);
        _dbContext.Database.EnsureCreated();

        _store = new SqliteProcessedDeliveryStore(_dbContext);
    }

    [Fact]
    public async Task HasProcessedAsync_ReturnsFalse_WhenDeliveryDoesNotExist()
    {
        var result = await _store.HasProcessedAsync(
            "delivery-1",
            CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task MarkProcessedAsync_SavesDelivery()
    {
        await _store.MarkProcessedAsync(
            "delivery-1",
            "order.created",
            """{"orderId":"ORD-1"}""",
            CancellationToken.None);

        var result = await _store.HasProcessedAsync(
            "delivery-1",
            CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task MarkProcessedAsync_IsIdempotent_ForDuplicateDeliveryId()
    {
        await _store.MarkProcessedAsync(
            "delivery-1",
            "order.created",
            """{"orderId":"ORD-1"}""",
            CancellationToken.None);

        await _store.MarkProcessedAsync(
            "delivery-1",
            "order.created",
            """{"orderId":"ORD-1"}""",
            CancellationToken.None);

        var count = await _dbContext.ProcessedDeliveries.CountAsync();

        Assert.Equal(1, count);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}