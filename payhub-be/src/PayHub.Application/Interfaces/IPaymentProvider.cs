namespace PayHub.Application.Interfaces
{
    public interface IPaymentProvider
    {
        Task<bool> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    }

    public class PaymentRequest
    {
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        // Diğer gerekli alanlar
    }
}
