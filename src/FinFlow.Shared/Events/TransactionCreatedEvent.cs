namespace FinFlow.Shared.Events;

public class TransactionCreatedEvent
{
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string SenderId { get; set; } = string.Empty;
    public string ReceiverId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
}
