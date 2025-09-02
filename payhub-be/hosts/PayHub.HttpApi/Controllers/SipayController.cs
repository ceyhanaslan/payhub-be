namespace PayHub.HttpApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using PayHub.Infrastructure.Adapters;

[ApiController]
[Route("api/[controller]")]
public class SipayController : ControllerBase
{
    private readonly SipayPaymentProvider _sipay;

    public SipayController(SipayPaymentProvider sipay)
    {
        _sipay = sipay;
    }

    [HttpPost("checkstatus")]
    public async Task<IActionResult> CheckStatus([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        var (content, statusCode) = await _sipay.CheckStatusAsync(json, cancellationToken);
        return StatusCode(statusCode, content);
    }

    [HttpPost("confirmPayment")]
    public async Task<IActionResult> ConfirmPayment([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        var (content, statusCode) = await _sipay.ConfirmPaymentAsync(json, cancellationToken);
        return StatusCode(statusCode, content);
    }

    [HttpPost("refund")]
    public async Task<IActionResult> Refund([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        var (content, statusCode) = await _sipay.RefundAsync(json, cancellationToken);
        return StatusCode(statusCode, content);
    }

    [HttpPost("saveCard")]
    public async Task<IActionResult> SaveCard([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        var (content, statusCode) = await _sipay.SaveCardAsync(json, cancellationToken);
        return StatusCode(statusCode, content);
    }

    [HttpPost("payByCardToken")]
    public async Task<IActionResult> PayByCardToken([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        var (content, statusCode) = await _sipay.PayByCardTokenAsync(json, cancellationToken);
        return StatusCode(statusCode, content);
    }

    [HttpPost("payByCardTokenNonSecure")]
    public async Task<IActionResult> PayByCardTokenNonSecure([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        var (content, statusCode) = await _sipay.PayByCardTokenNonSecureAsync(json, cancellationToken);
        return StatusCode(statusCode, content);
    }

    [HttpPost("getpos")]
    public async Task<IActionResult> GetPos([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        var (content, statusCode) = await _sipay.GetPosAsync(json, cancellationToken);
        return StatusCode(statusCode, content);
    }

    [HttpPost("installments")]
    public async Task<IActionResult> Installments([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        var (content, statusCode) = await _sipay.InstallmentsAsync(json, cancellationToken);
        return StatusCode(statusCode, content);
    }

    [HttpPost("commissions")]
    public async Task<IActionResult> Commissions([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        var (content, statusCode) = await _sipay.CommissionsAsync(json, cancellationToken);
        return StatusCode(statusCode, content);
    }

    [HttpPost("paySmart3D")]
    public async Task<IActionResult> PaySmart3D([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        var (content, statusCode) = await _sipay.PaySmart3DAsync(json, cancellationToken);
        return StatusCode(statusCode, content);
    }

    [HttpPost("complete")]
    public async Task<IActionResult> CompletePayment([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        var (content, statusCode) = await _sipay.CompletePaymentAsync(json, cancellationToken);
        return StatusCode(statusCode, content);
    }
}
