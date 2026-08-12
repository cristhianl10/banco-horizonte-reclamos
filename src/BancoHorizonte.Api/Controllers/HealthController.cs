using Microsoft.AspNetCore.Mvc;

namespace BancoHorizonte.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        service = "Banco Horizonte API",
        timestamp = DateTimeOffset.UtcNow
    });
}
