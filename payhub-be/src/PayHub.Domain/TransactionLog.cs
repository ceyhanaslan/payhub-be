namespace PayHub.Domain;
using System;

public class TransactionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionId { get; set; }
    public string State { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MaskedCardNumber { get; set; }
    public string? MaskedCvv { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? TraceId { get; set; }
}
