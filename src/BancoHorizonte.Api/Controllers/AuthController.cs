using BancoHorizonte.Api.Contracts;
using BancoHorizonte.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BancoHorizonte.Api.Controllers;

[ApiController, Route("api/auth"), Authorize]
public sealed class AuthController(AppDbContext db) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken ct)
    {
        var id = User.GetUserId();
        var user = await db.Users.AsNoTracking().Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct);
        if (user is null) return NotFound(new ProblemDetails { Title = "Perfil no configurado", Detail = "El usuario existe en Auth pero no tiene perfil activo en la aplicación." });
        return Ok(new CurrentUserResponse(user.Id, $"{user.FirstNames} {user.LastNames}", user.Email,
            user.UserRoles.Where(x => x.Role.IsActive).Select(x => x.Role.Name).ToList()));
    }
}
