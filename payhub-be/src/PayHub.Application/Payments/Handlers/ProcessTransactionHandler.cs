namespace PayHub.Application.Payments.Handlers;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using PayHub.Application.CQRS;
using PayHub.Application.Interfaces;
using PayHub.Application.Services;

public class ProcessTransactionHandler : ICommandHandler<Commands.ProcessTransactionCommand, bool>
{
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly RoutingEngine _routingEngine;

    public ProcessTransactionHandler(
        IEnumerable<IPaymentProvider> providers,
        RoutingEngine routingEngine)
    {
        _providers = providers;
        _routingEngine = routingEngine;
    }

    public async Task<bool> Handle(Commands.ProcessTransactionCommand command, CancellationToken cancellationToken = default)
    {
        // RoutingEngine ile uygun provider seçimi
        var selectedProvider = await _routingEngine.SelectProviderAsync(
            "merchant1", // Bu merchant ID config'ten gelmeli
            command.Request.BankCode,
            command.Request.Amount
        );

        if (selectedProvider == null)
        {
            return false;
        }

        return await selectedProvider.ProcessPaymentAsync(command.Request, cancellationToken);
    }
}
