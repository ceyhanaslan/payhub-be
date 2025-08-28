namespace PayHub.Application.Services;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using PayHub.Domain;

public interface ITransactionLogService
{
    Task LogAsync(Guid transactionId, string state, string message, string? cardNumber = null, string? cvv = null, string? traceId = null);
}

public class TransactionLogService : ITransactionLogService
{
    private static readonly ConcurrentBag<TransactionLog> _logs = new();

    public Task LogAsync(Guid transactionId, string state, string message, string? cardNumber = null, string? cvv = null, string? traceId = null)
    {
        var log = new TransactionLog
        {
            TransactionId = transactionId,
            State = state,
            Message = message,
            MaskedCardNumber = Mask(cardNumber),
            MaskedCvv = MaskCvv(cvv),
            TraceId = traceId
        };
        _logs.Add(log);
        return Task.CompletedTask;
    }

    private string? Mask(string? cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 6)
            return null;
        return cardNumber.Substring(0, 4) + new string('*', cardNumber.Length - 8) + cardNumber.Substring(cardNumber.Length - 4);
    }
    private string? MaskCvv(string? cvv)
    {
        if (string.IsNullOrEmpty(cvv)) return null;
        return new string('*', cvv.Length);
    }
}
