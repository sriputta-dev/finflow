using Confluent.Kafka;
using FinFlow.API.Data;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.API.Services;

/// <summary>
/// Background service that polls the outbox table and publishes unpublished
/// messages to Kafka. Runs every 2 seconds. Marks messages as processed after
/// successful publish. Retries on failure up to 5 times before abandoning.
/// </summary>
public class OutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<OutboxPublisher> _logger;

    private const int MaxRetries = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    public OutboxPublisher(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<OutboxPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.All,          // Wait for all replicas to acknowledge
            EnableIdempotence = true  // Kafka-level idempotent producer
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        _logger.LogInformation("Outbox publisher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxBatch(producer, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox batch processing error");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxBatch(IProducer<string, string> producer, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinFlowDbContext>();

        // Fetch unpublished messages that haven't exceeded retry limit
        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        if (!pending.Any()) return;

        _logger.LogInformation("Processing {Count} outbox messages", pending.Count);

        foreach (var msg in pending)
        {
            try
            {
                await producer.ProduceAsync(
                    msg.Topic,
                    new Message<string, string>
                    {
                        Key   = msg.Id.ToString(),
                        Value = msg.Payload
                    },
                    ct);

                msg.ProcessedAt = DateTime.UtcNow;
                msg.Error = null;
                _logger.LogInformation("Published outbox message {Id} to {Topic}", msg.Id, msg.Topic);
            }
            catch (ProduceException<string, string> ex)
            {
                msg.RetryCount++;
                msg.Error = ex.Error.Reason;
                _logger.LogWarning("Failed to publish message {Id}: {Reason} (retry {Count})",
                    msg.Id, ex.Error.Reason, msg.RetryCount);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
