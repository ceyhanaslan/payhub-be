using MediatR;
using PayHub.Application.Interfaces;

namespace PayHub.Application.Payments.Commands
{
    public class ProcessTransactionCommand : IRequest<bool>
    {
        public PaymentRequest Request { get; set; }
        public ProcessTransactionCommand(PaymentRequest request)
        {
            Request = request;
        }
    }
}
