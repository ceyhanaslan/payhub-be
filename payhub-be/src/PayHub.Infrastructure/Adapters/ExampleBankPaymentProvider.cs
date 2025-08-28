namespace PayHub.Infrastructure.Adapters;
using System.Threading;
using System.Threading.Tasks;

using PayHub.Application.Interfaces;

public class ExampleBankPaymentProvider : IPaymentProvider
{
    public async Task<bool> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        // Banka ile ödeme işlemi entegrasyonu örneği
        await Task.Delay(100, cancellationToken); // Simülasyon
        return true;
    }
}
