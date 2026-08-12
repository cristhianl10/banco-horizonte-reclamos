using System.Text.Json;
using BancoHorizonte.Api.Contracts;
using BancoHorizonte.Api.Domain;
using BancoHorizonte.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BancoHorizonte.Api.Application;

public sealed class ComplaintService(AppDbContext db, IPriorityAndSlaService calculator, TimeProvider clock)
{
    public async Task<(Complaint? Complaint, IReadOnlyList<ComplaintListItem> Duplicates)> CreateAsync(
        CreateComplaintRequest request, Guid actorId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == request.CategoryId && x.IsActive, ct)
            ?? throw new DomainRuleException("La categoría seleccionada no existe o está inactiva.");
        if (!await db.ReceptionChannels.AnyAsync(x => x.Id == request.ReceptionChannelId && x.IsActive, ct))
            throw new DomainRuleException("El canal seleccionado no existe o está inactivo.");
        if (request.SubcategoryId is not null && !await db.Subcategories.AnyAsync(
                x => x.Id == request.SubcategoryId && x.CategoryId == request.CategoryId && x.IsActive, ct))
            throw new DomainRuleException("La subcategoría no pertenece a la categoría seleccionada.");

        var normalizedDocument = request.Customer.DocumentNumber.Trim().ToUpperInvariant();
        var customer = await db.Customers.SingleOrDefaultAsync(x =>
            x.DocumentType == request.Customer.DocumentType.Trim().ToUpper() && x.DocumentNumber == normalizedDocument, ct);

        if (customer is not null)
        {
            var duplicateEntities = await db.Complaints.AsNoTracking()
                .Include(x => x.Customer).Include(x => x.Category).Include(x => x.Status).Include(x => x.Priority)
                .Where(x => x.CustomerId == customer.Id && x.CategoryId == request.CategoryId && x.ReceivedAt >= now.AddHours(-72))
                .OrderByDescending(x => x.ReceivedAt).Take(5).ToListAsync(ct);
            var duplicates = duplicateEntities.Select(x => MapListItem(x, now, calculator)).ToList();
            if (duplicates.Count > 0 && !request.ConfirmPossibleDuplicate) return (null, duplicates);
        }
        else
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(), DocumentType = request.Customer.DocumentType.Trim().ToUpperInvariant(),
                DocumentNumber = normalizedDocument, FirstNames = request.Customer.FirstNames.Trim(),
                LastNames = request.Customer.LastNames.Trim(), Email = NormalizeOptional(request.Customer.Email)?.ToLowerInvariant(),
                Phone = NormalizeOptional(request.Customer.Phone), CreatedAt = now, UpdatedAt = now
            };
            db.Customers.Add(customer);
        }

        var repeated = await db.Complaints.AnyAsync(x => x.CustomerId == customer.Id && x.CategoryId == request.CategoryId, ct);
        var calculated = calculator.Calculate(request.Impact, request.Urgency, category.Name, repeated);
        var priority = await db.Priorities.Where(x => x.IsActive && x.MinimumScore <= calculated.Score)
            .OrderByDescending(x => x.MinimumScore).FirstOrDefaultAsync(ct)
            ?? throw new DomainRuleException("No existe un nivel de prioridad configurado para el puntaje.");
        var policies = await db.SlaPolicies.AsNoTracking().Where(x => x.PriorityId == priority.Id && x.IsActive).ToListAsync(ct);
        var policy = calculator.SelectPolicy(policies, priority.Id, category.Id, now);
        var newStatus = await db.Statuses.SingleOrDefaultAsync(x => x.Name == "Nuevo" && x.IsActive, ct)
            ?? throw new DomainRuleException("No existe el estado inicial Nuevo.");

        var complaint = new Complaint
        {
            Id = Guid.NewGuid(), Code = $"BH-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            CustomerId = customer.Id, ReceptionChannelId = request.ReceptionChannelId, CategoryId = request.CategoryId,
            SubcategoryId = request.SubcategoryId, Description = request.Description.Trim(), Impact = request.Impact,
            Urgency = request.Urgency, StatusId = newStatus.Id, PriorityId = priority.Id,
            PriorityScore = calculated.Score, SlaPolicyId = policy.Id, ReceivedAt = now,
            SlaDeadline = calculator.CalculateDeadline(now, policy.ResolutionHours), CreatedByUserId = actorId,
            CreatedAt = now, UpdatedAt = now
        };
        db.Complaints.Add(complaint);
        db.History.Add(NewHistory(complaint.Id, actorId, "RECLAMO_CREADO", null, new { complaint.Code, calculated.Score, Priority = priority.Name }, now));
        await db.SaveChangesAsync(ct);
        return (complaint, []);
    }

    public async Task ChangeStatusAsync(Guid id, ChangeStatusRequest request, Guid actorId, CancellationToken ct)
    {
        var complaint = await db.Complaints.Include(x => x.Status).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ResourceNotFoundException("El reclamo no existe.");
        if (complaint.Status.IsFinal) throw new DomainRuleException("Un reclamo finalizado no puede cambiar de estado sin reapertura supervisada.");
        var target = await db.Statuses.SingleOrDefaultAsync(x => x.Id == request.StatusId && x.IsActive, ct)
            ?? throw new DomainRuleException("El estado de destino no existe o está inactivo.");
        if (!await db.StatusTransitions.AnyAsync(x => x.FromStatusId == complaint.StatusId && x.ToStatusId == target.Id, ct))
            throw new DomainRuleException($"No se permite cambiar de {complaint.Status.Name} a {target.Name}.");

        var now = clock.GetUtcNow();
        var previous = complaint.Status.Name;
        complaint.StatusId = target.Id;
        complaint.UpdatedAt = now;
        if (target.Name == "Resuelto") complaint.ResolvedAt = now;
        if (!string.IsNullOrWhiteSpace(request.Observation)) db.Observations.Add(new ComplaintObservation
        { Id = Guid.NewGuid(), ComplaintId = id, AuthorUserId = actorId, Content = request.Observation.Trim(), CreatedAt = now });
        db.History.Add(NewHistory(id, actorId, "ESTADO_CAMBIADO", new { Estado = previous }, new { Estado = target.Name }, now));
        await db.SaveChangesAsync(ct);
    }

    public async Task AssignAsync(Guid id, AssignComplaintRequest request, Guid actorId, CancellationToken ct)
    {
        var complaint = await db.Complaints.Include(x => x.Status).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ResourceNotFoundException("El reclamo no existe.");
        if (complaint.Status.IsFinal) throw new DomainRuleException("No se puede asignar un reclamo finalizado.");
        var analyst = await db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == request.AnalystId && x.IsActive, ct);
        if (analyst is null || !analyst.UserRoles.Any(x => x.Role.Name is "Analista" or "Supervisor"))
            throw new DomainRuleException("El responsable debe ser un analista o supervisor activo.");

        var now = clock.GetUtcNow();
        var active = await db.Assignments.SingleOrDefaultAsync(x => x.ComplaintId == id && x.EndedAt == null, ct);
        if (active?.AnalystId == request.AnalystId) throw new DomainRuleException("El usuario ya es responsable de este reclamo.");
        if (active is not null) active.EndedAt = now;
        var previousAssignee = complaint.CurrentAssigneeId;
        complaint.CurrentAssigneeId = request.AnalystId;
        if (complaint.Status.Name == "Nuevo")
        {
            var assignedStatus = await db.Statuses.SingleOrDefaultAsync(x => x.Name == "Asignado" && x.IsActive, ct)
                ?? throw new DomainRuleException("No existe el estado Asignado.");
            complaint.StatusId = assignedStatus.Id;
        }
        complaint.UpdatedAt = now;
        db.Assignments.Add(new ComplaintAssignment { Id = Guid.NewGuid(), ComplaintId = id, AnalystId = request.AnalystId, AssignedByUserId = actorId, AssignedAt = now, Reason = request.Reason?.Trim() });
        db.History.Add(NewHistory(id, actorId, previousAssignee is null ? "RECLAMO_ASIGNADO" : "RECLAMO_REASIGNADO", new { ResponsableId = previousAssignee }, new { ResponsableId = request.AnalystId }, now));
        await db.SaveChangesAsync(ct);
    }

    public async Task AddObservationAsync(Guid id, AddObservationRequest request, Guid actorId, CancellationToken ct)
    {
        if (!await db.Complaints.AnyAsync(x => x.Id == id, ct)) throw new ResourceNotFoundException("El reclamo no existe.");
        var now = clock.GetUtcNow();
        db.Observations.Add(new ComplaintObservation { Id = Guid.NewGuid(), ComplaintId = id, AuthorUserId = actorId, Content = request.Content.Trim(), IsInternal = request.IsInternal, CreatedAt = now });
        db.History.Add(NewHistory(id, actorId, "OBSERVACION_AGREGADA", null, new { request.IsInternal }, now));
        await db.SaveChangesAsync(ct);
    }

    internal static ComplaintListItem MapListItem(Complaint x, DateTimeOffset now, IPriorityAndSlaService calculator) => new(
        x.Id, x.Code, $"{x.Customer.FirstNames} {x.Customer.LastNames}", x.Category.Name, x.Status.Name, x.Priority.Name,
        x.CurrentAssignee is null ? null : $"{x.CurrentAssignee.FirstNames} {x.CurrentAssignee.LastNames}", x.ReceivedAt,
        x.SlaDeadline, calculator.GetSlaState(x.SlaDeadline, now, x.SlaPolicy?.AlertThresholdMinutes ?? 240));

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ComplaintHistory NewHistory(Guid complaintId, Guid actorId, string type, object? before, object? after, DateTimeOffset now) => new()
    {
        ComplaintId = complaintId, ActorUserId = actorId, EventType = type,
        BeforeData = before is null ? null : JsonSerializer.Serialize(before),
        AfterData = after is null ? null : JsonSerializer.Serialize(after), OccurredAt = now
    };
}
