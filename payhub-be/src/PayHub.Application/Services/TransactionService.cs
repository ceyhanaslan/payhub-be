using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using PayHub.Domain.Entities;

namespace PayHub.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private static readonly ConcurrentDictionary<string, Transaction> _idempotencyStore = new();
        private static readonly ConcurrentDictionary<Guid, Transaction> _transactions = new();


        #pragma warning disable CS1998
        public async Task<Transaction> StartTransactionAsync(string merchantId, string provider, decimal amount, string currency, string idempotencyKey)
        {
            if (_idempotencyStore.TryGetValue(idempotencyKey, out var existing))
                return existing;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                MerchantId = merchantId,
                Provider = provider,
                Amount = amount,
                Currency = currency,
                Status = TransactionStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                IdempotencyKey = idempotencyKey
            };
            _transactions[transaction.Id] = transaction;
            _idempotencyStore[idempotencyKey] = transaction;
            return transaction;
        }


        public async Task<Transaction> UpdateTransactionStatusAsync(Guid transactionId, TransactionStatus status, string? providerTransactionId = null, string? responseCode = null, string? responseMessage = null)
        {
            if (!_transactions.TryGetValue(transactionId, out var transaction))
                throw new Exception("Transaction not found");
            transaction.Status = status;
            transaction.UpdatedAt = DateTime.UtcNow;
            if (providerTransactionId != null) transaction.ProviderTransactionId = providerTransactionId;
            if (responseCode != null) transaction.ResponseCode = responseCode;
            if (responseMessage != null) transaction.ResponseMessage = responseMessage;
            return transaction;
        }

        public async Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey)
        {
            _idempotencyStore.TryGetValue(idempotencyKey, out var transaction);
            return transaction;
        }
#pragma warning restore CS1998
    }
}
