namespace FinFlow.Shared.Events;

public class PaymentProcessedEvent
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = string.Empty; // Approved | Declined | Flagged
    public string Reason { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
