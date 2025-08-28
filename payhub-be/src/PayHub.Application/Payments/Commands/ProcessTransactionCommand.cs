namespace PayHub.Application.Payments.Commands;


using System.Threading;
using System.Threading.Tasks;

using PayHub.Application.CQRS;
using PayHub.Application.Interfaces;
using PayHub.Application.Services;
using PayHub.Domain.Entities;

public class ProcessTransactionCommand : ICommand<bool>
{
    public PaymentRequest Request { get; }
    public string IdempotencyKey { get; }
    public ProcessTransactionCommand(PaymentRequest request, string idempotencyKey)
    {
        Request = request;
        IdempotencyKey = idempotencyKey;
    }
}

public class ProcessTransactionHandler : ICommandHandler<ProcessTransactionCommand, bool>
{
    private readonly IPaymentProvider _paymentProvider;
    private readonly ITransactionService _transactionService;
    public ProcessTransactionHandler(IPaymentProvider paymentProvider, ITransactionService transactionService)
    {
        _paymentProvider = paymentProvider;
        _transactionService = transactionService;
    }

    public async Task<bool> Handle(ProcessTransactionCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await _transactionService.GetByIdempotencyKeyAsync(command.IdempotencyKey);
        if (existing != null && existing.Status != TransactionStatus.Pending)
            return existing.Status == TransactionStatus.Approved;

        var transaction = await _transactionService.StartTransactionAsync(
            command.Request.TransactionId,
            command.Request.BankCode,
            command.Request.Amount,
            command.Request.Currency,
            command.IdempotencyKey);

        await _transactionService.UpdateTransactionStatusAsync(transaction.Id, TransactionStatus.Processing);

        var result = await _paymentProvider.ProcessPaymentAsync(command.Request, cancellationToken);
        var newStatus = result ? TransactionStatus.Approved : TransactionStatus.Declined;

        await _transactionService.UpdateTransactionStatusAsync(transaction.Id, newStatus);

        return result;
    }
}
