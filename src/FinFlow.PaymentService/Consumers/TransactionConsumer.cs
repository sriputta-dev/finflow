using System.Text.Json;
using Confluent.Kafka;
using FinFlow.Shared.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinFlow.PaymentService;

/// <summary>
/// Consumes TransactionCreatedEvent from Kafka.
/// Applies business rules (fraud checks, balance validation) and publishes PaymentProcessedEvent.
/// Uses a deduplication store to guarantee idempotent processing under at-least-once delivery.
/// </summary>
public class TransactionConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<TransactionConsumer> _logger;

    // In-memory deduplication store (use Redis or DB in production)
    private readonly HashSet<string> _processedKeys = new();

    public TransactionConsumer(IConfiguration config, ILogger<TransactionConsumer> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers  = _config["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId           = "payment-service-group",
            AutoOffsetReset   = AutoOffsetReset.Earliest,
            EnableAutoCommit  = false  // Manual commit after successful processing
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.All
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        consumer.Subscribe("transaction.created");
        _logger.LogInformation("PaymentService listening on transaction.created");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result == null) continue;

                var evt = JsonSerializer.Deserialize<TransactionCreatedEvent>(result.Message.Value);
                if (evt == null) continue;

                // Idempotency check — skip if we've already processed this key
                if (_processedKeys.Contains(evt.IdempotencyKey))
                {
                    _logger.LogWarning("Skipping duplicate event for key {Key}", evt.IdempotencyKey);
                    consumer.Commit(result);
                    continue;
                }

                _logger.LogInformation("Processing transaction {Id} — Amount: {Amount} {Currency}",
                    evt.TransactionId, evt.Amount, evt.Currency);

                // Business rule evaluation
                var (status, reason) = EvaluateTransaction(evt);

                // Simulate async processing (DB write, fraud API call, etc.)
                await Task.Delay(200, stoppingToken);

                // Publish result to downstream topic
                var processedEvt = new PaymentProcessedEvent
                {
                    TransactionId = evt.TransactionId,
                    Status        = status,
                    Reason        = reason,
                    ProcessedAt   = DateTime.UtcNow
                };

                await producer.ProduceAsync(
                    "payment.processed",
                    new Message<string, string>
                    {
                        Key   = evt.TransactionId.ToString(),
                        Value = JsonSerializer.Serialize(processedEvt)
                    },
                    stoppingToken);

                _processedKeys.Add(evt.IdempotencyKey);

                // Commit offset only after successful processing and publish
                consumer.Commit(result);

                _logger.LogInformation("Transaction {Id} processed with status: {Status}",
                    evt.TransactionId, status);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing transaction");
            }
        }

        consumer.Close();
    }

    /// <summary>
    /// Applies business rules to classify the transaction.
    /// In production this would call a fraud detection service, check account balances, etc.
    /// </summary>
    private static (string Status, string Reason) EvaluateTransaction(TransactionCreatedEvent evt)
    {
        // Flag large transactions for manual review
        if (evt.Amount > 50_000)
            return ("Flagged", "Amount exceeds automatic approval threshold — requires manual review");

        // Decline same-account transfers
        if (evt.SenderId == evt.ReceiverId)
            return ("Declined", "Sender and receiver cannot be the same account");

        // Decline zero/negative (defensive — API should catch this too)
        if (evt.Amount <= 0)
            return ("Declined", "Invalid transaction amount");

        return ("Approved", "Passed all automated checks");
    }
}
