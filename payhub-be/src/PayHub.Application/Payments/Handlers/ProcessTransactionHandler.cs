namespace PayHub.Application.Payments.Handlers;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using PayHub.Application.CQRS;
using PayHub.Application.Interfaces;

public class ProcessTransactionHandler : ICommandHandler<Commands.ProcessTransactionCommand, bool>
{
    private readonly IEnumerable<IPaymentProvider> _providers;
    public ProcessTransactionHandler(IEnumerable<IPaymentProvider> providers)
    {
        _providers = providers;
    }

    public async Task<bool> Handle(Commands.ProcessTransactionCommand command, CancellationToken cancellationToken = default)
    {
        // Banka koduna göre uygun provider seçimi
        var provider = _providers.FirstOrDefault(p => p.GetType().Name.Contains(command.Request.BankCode));
        if (provider == null) return false;
        return await provider.ProcessPaymentAsync(command.Request, cancellationToken);
    }
}
