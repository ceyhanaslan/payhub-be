using MediatR;
using Microsoft.AspNetCore.Mvc;
using PayHub.Application.Payments.Commands;
using PayHub.Application.Interfaces;

namespace PayHub.HttpApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly IMediator _mediator;
        public TransactionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Process([FromBody] PaymentRequest request)
        {
            var result = await _mediator.Send(new ProcessTransactionCommand(request));
            if (result)
                return Ok("Transaction processed successfully.");
            return BadRequest("Transaction failed.");
        }
    }
}
