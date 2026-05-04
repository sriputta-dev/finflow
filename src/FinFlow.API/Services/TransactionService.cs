using System.Text.Json;
using FinFlow.API.Data;
using FinFlow.API.Hubs;
using FinFlow.API.Models;
using FinFlow.Shared.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.API.Services;

public interface ITransactionService
{
    Task<Transaction> CreateAsync(CreateTransactionRequest request);
    Task<List<Transaction>> GetRecentAsync(int count = 50);
    Task UpdateStatusAsync(Guid transactionId, string status);
}

public record CreateTransactionRequest(
    decimal Amount,
    string Currency,
    string SenderId,
    string ReceiverId,
    string Description,
    string? IdempotencyKey = null);

public class TransactionService : ITransactionService
{
    private readonly FinFlowDbContext _db;
    private readonly IHubContext<DashboardHub> _hub;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        FinFlowDbContext db,
        IHubContext<DashboardHub> hub,
        ILogger<TransactionService> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    public async Task<Transaction> CreateAsync(CreateTransactionRequest request)
    {
        var idempotencyKey = request.IdempotencyKey ?? Guid.NewGuid().ToString();

        // Check idempotency — return existing if duplicate key
        var existing = await _db.Transactions
            .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);

        if (existing != null)
        {
            _logger.LogWarning("Duplicate transaction request with key {Key}", idempotencyKey);
            return existing;
        }

        var transaction = new Transaction
        {
            Amount     = request.Amount,
            Currency   = request.Currency,
            SenderId   = request.SenderId,
            ReceiverId = request.ReceiverId,
            Description = request.Description,
            IdempotencyKey = idempotencyKey,
            Status = "Pending"
        };

        // Build the outbox event payload
        var evt = new TransactionCreatedEvent
        {
            TransactionId   = transaction.Id,
            Amount          = transaction.Amount,
            Currency        = transaction.Currency,
            SenderId        = transaction.SenderId,
            ReceiverId      = transaction.ReceiverId,
            Description     = transaction.Description,
            CreatedAt       = transaction.CreatedAt,
            IdempotencyKey  = idempotencyKey
        };

        var outbox = new OutboxMessage
        {
            Topic   = "transaction.created",
            Payload = JsonSerializer.Serialize(evt)
        };

        // Single atomic write — both transaction AND outbox message saved together
        // This is the core of the Outbox Pattern: if this transaction commits,
        // the event WILL eventually be published. If it rolls back, nothing is published.
        await _db.Transactions.AddAsync(transaction);
        await _db.OutboxMessages.AddAsync(outbox);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Transaction {Id} created and queued to outbox", transaction.Id);

        // Push to SignalR dashboard immediately (best-effort, non-critical)
        await _hub.Clients.All.SendAsync("TransactionCreated", new
        {
            transaction.Id,
            transaction.Amount,
            transaction.Currency,
            transaction.SenderId,
            transaction.ReceiverId,
            transaction.Status,
            transaction.CreatedAt
        });

        return transaction;
    }

    public async Task<List<Transaction>> GetRecentAsync(int count = 50)
        => await _db.Transactions
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task UpdateStatusAsync(Guid transactionId, string status)
    {
        var tx = await _db.Transactions.FindAsync(transactionId);
        if (tx == null) return;

        tx.Status = status;
        tx.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Push status update to all connected dashboard clients
        await _hub.Clients.All.SendAsync("TransactionUpdated", new
        {
            tx.Id,
            tx.Status,
            tx.UpdatedAt
        });
    }
}
