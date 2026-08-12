using BancoHorizonte.Api.Contracts;
using BancoHorizonte.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BancoHorizonte.Api.Controllers;

[ApiController, Route("api/catalogos"), Authorize]
public sealed class CatalogsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CatalogResponse>> Get(CancellationToken ct)
    {
        var channels = await db.ReceptionChannels.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CatalogItem(x.Id, x.Name)).ToListAsync(ct);
        var categories = await db.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new CatalogItem(x.Id, x.Name)).ToListAsync(ct);
        var subcategories = await db.Subcategories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new SubcategoryItem(x.Id, x.Name, x.CategoryId)).ToListAsync(ct);
        var statuses = await db.Statuses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Order).Select(x => new CatalogItem(x.Id, x.Name)).ToListAsync(ct);
        var priorities = await db.Priorities.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Order).Select(x => new CatalogItem(x.Id, x.Name)).ToListAsync(ct);
        var analysts = await db.Users.AsNoTracking().Where(x => x.IsActive && x.UserRoles.Any(r => r.Role.Name == "Analista" || r.Role.Name == "Supervisor"))
            .OrderBy(x => x.FirstNames).Select(x => new AnalystItem(x.Id, x.FirstNames + " " + x.LastNames, x.Email)).ToListAsync(ct);
        return Ok(new CatalogResponse(channels, categories, subcategories, statuses, priorities, analysts));
    }
}
