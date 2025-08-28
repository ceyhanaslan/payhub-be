namespace PayHub.Domain.Entities;
using System;

public enum TransactionStatus
{
    Pending,
    Processing,
    Approved,
    Declined,
    Settled,
    Reconciled
}

public class Transaction
{
    public Guid Id { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string ProviderTransactionId { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public string ResponseMessage { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}
