using Microsoft.AspNetCore.Mvc;
using PayHub.Application.Payments.Commands;
using PayHub.Application.CQRS;
using PayHub.Application.Interfaces;

namespace PayHub.HttpApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ICommandDispatcher _commandDispatcher;
        public TransactionController(ICommandDispatcher commandDispatcher)
        {
            _commandDispatcher = commandDispatcher;
        }

        [HttpPost]
        public async Task<IActionResult> Process([FromBody] PaymentRequest request, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return BadRequest("Idempotency-Key header is required.");
            var result = await _commandDispatcher.Dispatch<bool>(new ProcessTransactionCommand(request, idempotencyKey));
            if (result)
                return Ok("Transaction processed successfully.");
            return BadRequest("Transaction failed.");
        }
    }
}
