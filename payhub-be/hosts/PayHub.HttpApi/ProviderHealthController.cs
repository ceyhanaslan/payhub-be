using Microsoft.AspNetCore.Mvc;
using System.Linq;

[ApiController]
[Route("api/provider-health")]
public class ProviderHealthController : ControllerBase
{
    private readonly ProviderHealthService _service;
    public ProviderHealthController(ProviderHealthService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetAll() => Ok(_service.GetAll());

    [HttpGet("{provider}")]
    public IActionResult Get(string provider)
    {
        var h = _service.Get(provider);
        if (h == null) return NotFound();
        return Ok(h);
    }
}
