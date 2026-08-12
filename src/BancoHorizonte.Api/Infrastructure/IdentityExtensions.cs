using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace BancoHorizonte.Api.Infrastructure;

public static class IdentityExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException("El token no contiene un identificador de usuario válido.");
    }
}

public sealed class DatabaseRoleClaimsTransformation(AppDbContext db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated) return principal;
        var applicationRoles = new[] { "Operador", "Analista", "Supervisor", "Administrador" };
        if (identity.Claims.Any(x => x.Type == ClaimTypes.Role && applicationRoles.Contains(x.Value))) return principal;
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(value, out var userId)) return principal;

        var roles = await db.UserRoles.AsNoTracking().Where(x => x.UserId == userId && x.Role.IsActive)
            .Select(x => x.Role.Name).ToListAsync();
        foreach (var role in roles) identity.AddClaim(new Claim(ClaimTypes.Role, role));
        return principal;
    }
}
