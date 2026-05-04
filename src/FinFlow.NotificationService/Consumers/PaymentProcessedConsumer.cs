using System.Text.Json;
using Confluent.Kafka;
using FinFlow.Shared.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinFlow.NotificationService;

/// <summary>
/// Consumes PaymentProcessedEvent from Kafka.
/// Responsible for audit logging and simulating downstream notifications (email, SMS).
/// Fully independent — knows nothing about the API or PaymentService internals.
/// </summary>
public class PaymentProcessedConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentProcessedConsumer> _logger;

    public PaymentProcessedConsumer(IConfiguration config, ILogger<PaymentProcessedConsumer> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _config["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId          = "notification-service-group",
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe("payment.processed");
        _logger.LogInformation("NotificationService listening on payment.processed");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result == null) continue;

                var evt = JsonSerializer.Deserialize<PaymentProcessedEvent>(result.Message.Value);
                if (evt == null) continue;

                await HandleNotification(evt, stoppingToken);

                consumer.Commit(result);
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
            }
        }

        consumer.Close();
    }

    private async Task HandleNotification(PaymentProcessedEvent evt, CancellationToken ct)
    {
        // Audit log — in production this writes to a separate audit DB
        _logger.LogInformation(
            "[AUDIT] Transaction {Id} | Status: {Status} | Reason: {Reason} | ProcessedAt: {Time}",
            evt.TransactionId, evt.Status, evt.Reason, evt.ProcessedAt);

        // Simulate sending notification (email/SMS service call)
        var message = evt.Status switch
        {
            "Approved" => $"Your payment {evt.TransactionId} has been approved.",
            "Declined" => $"Your payment {evt.TransactionId} was declined: {evt.Reason}",
            "Flagged"  => $"Your payment {evt.TransactionId} is under review.",
            _          => $"Payment {evt.TransactionId} status: {evt.Status}"
        };

        await Task.Delay(50, ct); // Simulate async notification dispatch

        _logger.LogInformation("[NOTIFY] → {Message}", message);
    }
}
