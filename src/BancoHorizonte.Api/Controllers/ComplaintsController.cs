using BancoHorizonte.Api.Application;
using BancoHorizonte.Api.Contracts;
using BancoHorizonte.Api.Domain;
using BancoHorizonte.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BancoHorizonte.Api.Controllers;

[ApiController, Route("api/reclamos"), Authorize]
public sealed class ComplaintsController(AppDbContext db, ComplaintService service, IPriorityAndSlaService calculator, TimeProvider clock) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ComplaintListItem>>> List([FromQuery] ComplaintQuery query, CancellationToken ct)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            return BadRequest(new ProblemDetails { Title = "Paginación inválida", Detail = "Page debe ser >= 1 y PageSize debe estar entre 1 y 100." });

        var actorId = User.GetUserId();
        await service.RefreshOpenPrioritiesAsync(actorId, ct);
        var source = db.Complaints.AsNoTracking().Include(x => x.Customer).Include(x => x.Category).Include(x => x.Status)
            .Include(x => x.Priority).Include(x => x.CurrentAssignee).AsQueryable();
        if (!User.IsInRole("Supervisor") && !User.IsInRole("Administrador"))
        {
            source = User.IsInRole("Analista")
                ? source.Where(x => x.CurrentAssigneeId == actorId || x.CurrentAssigneeId == null)
                : source.Where(x => x.CreatedByUserId == actorId);
        }
        if (query.StatusId is not null) source = source.Where(x => x.StatusId == query.StatusId);
        if (query.PriorityId is not null) source = source.Where(x => x.PriorityId == query.PriorityId);
        if (query.CategoryId is not null) source = source.Where(x => x.CategoryId == query.CategoryId);
        if (query.ChannelId is not null) source = source.Where(x => x.ReceptionChannelId == query.ChannelId);
        if (query.AssigneeId is not null) source = source.Where(x => x.CurrentAssigneeId == query.AssigneeId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            source = source.Where(x => x.Code.ToLower().Contains(search) || x.Customer.DocumentNumber.ToLower().Contains(search) ||
                x.Customer.FirstNames.ToLower().Contains(search) || x.Customer.LastNames.ToLower().Contains(search));
        }
        var now = clock.GetUtcNow();
        if (query.Sla?.Equals("overdue", StringComparison.OrdinalIgnoreCase) == true)
            source = source.Where(x => x.SlaDeadline <= now && !x.Status.IsFinal);
        if (query.Sla?.Equals("near", StringComparison.OrdinalIgnoreCase) == true)
            source = source.Where(x => x.SlaDeadline > now && x.SlaAlertAt <= now && !x.Status.IsFinal);

        var total = await source.CountAsync(ct);
        var entities = await source.OrderByDescending(x => x.Priority.Order).ThenBy(x => x.SlaDeadline)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        return Ok(new PagedResult<ComplaintListItem>(entities.Select(x => ComplaintService.MapListItem(x, now, calculator)).ToList(),
            query.Page, query.PageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ComplaintDetail>> Detail(Guid id, CancellationToken ct)
    {
        await service.RefreshOpenPrioritiesAsync(User.GetUserId(), ct);
        var x = await db.Complaints.AsNoTracking().Include(x => x.Customer).Include(x => x.ReceptionChannel).Include(x => x.Category)
            .Include(x => x.Subcategory).Include(x => x.Status).Include(x => x.Priority).Include(x => x.CurrentAssignee)
            .Include(x => x.Observations).ThenInclude(x => x.Author).Include(x => x.History).ThenInclude(x => x.Actor)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
        if (x is null) return NotFound();
        if (!CanView(x)) return Forbid();
        var timeline = x.History.Select(h => new TimelineItem(h.EventType, h.AfterData ?? "Sin detalle",
                $"{h.Actor.FirstNames} {h.Actor.LastNames}", h.OccurredAt))
            .Concat(x.Observations.Select(o => new TimelineItem("OBSERVACION", o.Content,
                $"{o.Author.FirstNames} {o.Author.LastNames}", o.CreatedAt)))
            .OrderByDescending(item => item.At).ToList();
        return Ok(new ComplaintDetail(x.Id, x.Code, $"{x.Customer.FirstNames} {x.Customer.LastNames}",
            $"{x.Customer.DocumentType} {x.Customer.DocumentNumber}", x.Customer.Email, x.Customer.Phone,
            x.ReceptionChannel.Name, x.Category.Name, x.Subcategory?.Name, x.Description, x.AffectedAmount,
            x.DigitalChannelUnavailable, x.Status.Name, x.StatusId, x.Priority.Name, x.PriorityScore,
            x.CurrentAssignee is null ? null : $"{x.CurrentAssignee.FirstNames} {x.CurrentAssignee.LastNames}",
            x.CurrentAssigneeId, x.ReceivedAt, x.SlaDeadline,
            calculator.GetSlaState(x.SlaDeadline, x.SlaAlertAt, clock.GetUtcNow()),
            ComplaintService.ReadPriorityRules(x.PriorityBreakdown), timeline));
    }

    [HttpPost, Authorize(Roles = "Operador,Supervisor,Administrador")]
    public async Task<IActionResult> Create(CreateComplaintRequest request, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, User.GetUserId(), ct);
        if (result.Complaint is null)
            return Conflict(new { message = "Se encontraron posibles reclamos duplicados.", possibleDuplicates = result.Duplicates });
        var complaint = result.Complaint;
        var priority = await db.Priorities.AsNoTracking().Where(x => x.Id == complaint.PriorityId).Select(x => x.Name).SingleAsync(ct);
        return CreatedAtAction(nameof(Detail), new { id = complaint.Id }, new
        {
            complaint.Id, complaint.Code, complaint.PriorityScore, Priority = priority, complaint.SlaDeadline,
            PriorityRules = ComplaintService.ReadPriorityRules(complaint.PriorityBreakdown)
        });
    }

    [HttpPatch("{id:guid}/estado"), Authorize(Roles = "Analista,Supervisor,Administrador")]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeStatusRequest request, CancellationToken ct)
    {
        var actor = User.GetUserId();
        if (!await CanManageAsync(id, actor, allowUnassigned: false, ct)) return Forbid();
        await service.ChangeStatusAsync(id, request, actor, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/asignaciones"), Authorize(Roles = "Supervisor,Administrador")]
    public async Task<IActionResult> Assign(Guid id, AssignComplaintRequest request, CancellationToken ct)
    {
        await service.AssignAsync(id, request, User.GetUserId(), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/asumir"), Authorize(Roles = "Analista,Supervisor")]
    public async Task<IActionResult> Take(Guid id, CancellationToken ct)
    {
        var actor = User.GetUserId();
        if (!await CanManageAsync(id, actor, allowUnassigned: true, ct)) return Forbid();
        await service.AssignAsync(id, new AssignComplaintRequest(actor, "Caso asumido por el analista"), actor, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/observaciones"), Authorize(Roles = "Analista,Supervisor,Administrador")]
    public async Task<IActionResult> AddObservation(Guid id, AddObservationRequest request, CancellationToken ct)
    {
        var actor = User.GetUserId();
        if (!await CanManageAsync(id, actor, allowUnassigned: false, ct)) return Forbid();
        await service.AddObservationAsync(id, request, actor, ct);
        return NoContent();
    }

    private bool CanView(Complaint complaint)
    {
        if (User.IsInRole("Supervisor") || User.IsInRole("Administrador")) return true;
        var actor = User.GetUserId();
        if (User.IsInRole("Analista")) return complaint.CurrentAssigneeId is null || complaint.CurrentAssigneeId == actor;
        return complaint.CreatedByUserId == actor;
    }

    private async Task<bool> CanManageAsync(Guid complaintId, Guid actorId, bool allowUnassigned, CancellationToken ct)
    {
        if (User.IsInRole("Supervisor") || User.IsInRole("Administrador")) return true;
        var assigneeId = await db.Complaints.AsNoTracking().Where(x => x.Id == complaintId)
            .Select(x => x.CurrentAssigneeId).SingleOrDefaultAsync(ct);
        return assigneeId == actorId || (allowUnassigned && assigneeId is null);
    }
}
