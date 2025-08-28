namespace PayHub.Application.Interfaces
{
    /// <summary>
    /// Her ödeme sağlayıcı için adapter implementasyonu yapılır.
    /// TransactionService ile state machine yönetimi ve idempotency TransactionService üzerinden sağlanır.
    /// </summary>
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
    }
}
