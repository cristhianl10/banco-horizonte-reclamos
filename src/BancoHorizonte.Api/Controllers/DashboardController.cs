using BancoHorizonte.Api.Application;
using BancoHorizonte.Api.Contracts;
using BancoHorizonte.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BancoHorizonte.Api.Controllers;

[ApiController, Route("api/dashboard"), Authorize(Roles = "Supervisor,Administrador")]
public sealed class DashboardController(AppDbContext db, IPriorityAndSlaService calculator, TimeProvider clock) : ControllerBase
{
    [HttpGet("resumen")]
    public async Task<ActionResult<DashboardSummary>> Summary(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var openQuery = db.Complaints.AsNoTracking().Where(x => !x.Status.IsFinal);
        var open = await openQuery.CountAsync(ct);
        var critical = await openQuery.CountAsync(x => x.Priority.Name == "Crítica", ct);
        var near = await openQuery.CountAsync(x => x.SlaDeadline > now && x.SlaDeadline <= now.AddHours(4), ct);
        var overdue = await openQuery.CountAsync(x => x.SlaDeadline <= now, ct);
        var resolved = await db.Complaints.AsNoTracking().CountAsync(x => x.ResolvedAt != null, ct);
        var compliant = await db.Complaints.AsNoTracking().CountAsync(x => x.ResolvedAt != null && x.ResolvedAt <= x.SlaDeadline, ct);
        var byStatus = await db.Complaints.AsNoTracking().GroupBy(x => x.Status.Name).Select(g => new MetricSlice(g.Key, g.Count())).ToListAsync(ct);
        var byCategory = await db.Complaints.AsNoTracking().GroupBy(x => x.Category.Name).Select(g => new MetricSlice(g.Key, g.Count())).ToListAsync(ct);
        var loads = await openQuery.Where(x => x.CurrentAssignee != null).GroupBy(x => x.CurrentAssignee!.FirstNames + " " + x.CurrentAssignee.LastNames)
            .Select(g => new AnalystLoad(g.Key, g.Count())).OrderByDescending(x => x.ActiveCases).ToListAsync(ct);
        var immediateEntities = await openQuery.Include(x => x.Customer).Include(x => x.Category).Include(x => x.Status).Include(x => x.Priority)
            .Include(x => x.CurrentAssignee).Include(x => x.SlaPolicy).Where(x => x.SlaDeadline <= now.AddHours(4) || x.Priority.Name == "Crítica")
            .OrderBy(x => x.SlaDeadline).Take(8).ToListAsync(ct);
        return Ok(new DashboardSummary(open, critical, near, overdue, resolved == 0 ? 100 : Math.Round(compliant * 100m / resolved, 1),
            byStatus, byCategory, loads, immediateEntities.Select(x => ComplaintService.MapListItem(x, now, calculator)).ToList()));
    }
}
