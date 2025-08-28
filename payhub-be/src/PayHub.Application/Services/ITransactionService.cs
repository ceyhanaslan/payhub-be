namespace PayHub.Application.Services;
using System.Threading.Tasks;

using PayHub.Domain.Entities;

public interface ITransactionService
{
    Task<Transaction> StartTransactionAsync(string merchantId, string provider, decimal amount, string currency, string idempotencyKey);
    Task<Transaction> UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status, string? providerTransactionId = null, string? responseCode = null, string? responseMessage = null);
    Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey);
}
