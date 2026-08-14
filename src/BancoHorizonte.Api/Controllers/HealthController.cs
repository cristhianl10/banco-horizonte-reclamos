using BancoHorizonte.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BancoHorizonte.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(AppDbContext db, ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        service = "Banco Horizonte API",
        timestamp = DateTimeOffset.UtcNow
    });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        bool databaseAvailable;
        try
        {
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync(ct);
            await connection.CloseAsync();
            databaseAvailable = true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database readiness check failed");
            databaseAvailable = false;
        }
        return databaseAvailable
            ? Ok(new { status = "ready", database = "connected", timestamp = DateTimeOffset.UtcNow })
            : StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { status = "not-ready", database = "unavailable", timestamp = DateTimeOffset.UtcNow });
    }
}
