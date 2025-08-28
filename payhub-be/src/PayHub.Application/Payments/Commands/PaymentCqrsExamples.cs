using PayHub.Application.CQRS;

public class CreatePaymentCommand : ICommand<Guid>
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    // Diğer alanlar...
}

public class CreatePaymentCommandHandler : ICommandHandler<CreatePaymentCommand, Guid>
{
    public Task<Guid> Handle(CreatePaymentCommand command, CancellationToken cancellationToken = default)
    {
        // Ödeme kaydı oluştur
        var paymentId = Guid.NewGuid();
        // ... DB kaydı vs.
        return Task.FromResult(paymentId);
    }
}

public class GetPaymentByIdQuery : IQuery<PaymentDto>
{
    public Guid PaymentId { get; set; }
}

public class GetPaymentByIdQueryHandler : IQueryHandler<GetPaymentByIdQuery, PaymentDto>
{
    public Task<PaymentDto> Handle(GetPaymentByIdQuery query, CancellationToken cancellationToken = default)
    {
        // ... DB'den oku
        var dto = new PaymentDto { Id = query.PaymentId, Amount = 100, Currency = "TRY" };
        return Task.FromResult(dto);
    }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
}
