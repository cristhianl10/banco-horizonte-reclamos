using System.Text.Json;
using BancoHorizonte.Api.Contracts;
using BancoHorizonte.Api.Domain;
using BancoHorizonte.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BancoHorizonte.Api.Application;

public sealed class ComplaintService(AppDbContext db, IPriorityAndSlaService calculator, TimeProvider clock)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<(Complaint? Complaint, IReadOnlyList<ComplaintListItem> Duplicates)> CreateAsync(
        CreateComplaintRequest request, Guid actorId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var receivedAt = request.ReceivedAt?.ToUniversalTime() ?? now;
        var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == request.CategoryId && x.IsActive, ct)
            ?? throw new DomainRuleException("La categoría seleccionada no existe o está inactiva.");
        if (!await db.ReceptionChannels.AnyAsync(x => x.Id == request.ReceptionChannelId && x.IsActive, ct))
            throw new DomainRuleException("El canal seleccionado no existe o está inactivo.");
        var subcategory = request.SubcategoryId is null ? null : await db.Subcategories.SingleOrDefaultAsync(
            x => x.Id == request.SubcategoryId && x.CategoryId == request.CategoryId && x.IsActive, ct);
        if (request.SubcategoryId is not null && subcategory is null)
            throw new DomainRuleException("La subcategoría no pertenece a la categoría seleccionada.");

        var normalizedDocument = request.Customer.DocumentNumber.Trim().ToUpperInvariant();
        var documentType = request.Customer.DocumentType.Trim().ToUpperInvariant();
        var customer = await db.Customers.SingleOrDefaultAsync(x =>
            x.DocumentType == documentType && x.DocumentNumber == normalizedDocument, ct);

        if (customer is not null)
        {
            var duplicateEntities = await db.Complaints.AsNoTracking()
                .Include(x => x.Customer).Include(x => x.Category).Include(x => x.Status).Include(x => x.Priority)
                .Include(x => x.CurrentAssignee)
                .Where(x => x.CustomerId == customer.Id && x.CategoryId == request.CategoryId && x.ReceivedAt >= now.AddHours(-72))
                .OrderByDescending(x => x.ReceivedAt).Take(5).ToListAsync(ct);
            var duplicates = duplicateEntities.Select(x => MapListItem(x, now, calculator)).ToList();
            if (duplicates.Count > 0 && !request.ConfirmPossibleDuplicate) return (null, duplicates);
            customer.FirstNames = request.Customer.FirstNames.Trim();
            customer.LastNames = request.Customer.LastNames.Trim();
            customer.Email = NormalizeOptional(request.Customer.Email)?.ToLowerInvariant();
            customer.Phone = NormalizeOptional(request.Customer.Phone);
            customer.UpdatedAt = now;
        }
        else
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(), DocumentType = documentType, DocumentNumber = normalizedDocument,
                FirstNames = request.Customer.FirstNames.Trim(), LastNames = request.Customer.LastNames.Trim(),
                Email = NormalizeOptional(request.Customer.Email)?.ToLowerInvariant(), Phone = NormalizeOptional(request.Customer.Phone),
                CreatedAt = now, UpdatedAt = now
            };
            db.Customers.Add(customer);
        }

        var calculated = calculator.Calculate(new PriorityInput(category.Name, subcategory?.Name, request.AffectedAmount,
            request.DigitalChannelUnavailable, receivedAt, now));
        var priority = await FindPriorityAsync(calculated, ct);
        var policy = await FindPolicyAsync(priority.Id, category.Id, now, calculated.SlaHours, ct);
        var newStatus = await db.Statuses.SingleOrDefaultAsync(x => x.Name == "Nuevo" && x.IsActive, ct)
            ?? throw new DomainRuleException("No existe el estado inicial Nuevo.");
        var deadline = calculator.CalculateDeadline(receivedAt, calculated.SlaHours);

        var complaint = new Complaint
        {
            Id = Guid.NewGuid(), Code = $"BH-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            CustomerId = customer.Id, ReceptionChannelId = request.ReceptionChannelId, CategoryId = request.CategoryId,
            SubcategoryId = request.SubcategoryId, Description = request.Description.Trim(), AffectedAmount = request.AffectedAmount,
            DigitalChannelUnavailable = request.DigitalChannelUnavailable, StatusId = newStatus.Id, PriorityId = priority.Id,
            PriorityScore = calculated.Score, PriorityBreakdown = JsonSerializer.Serialize(calculated.MatchedRules, JsonOptions),
            SlaPolicyId = policy.Id, ReceivedAt = receivedAt, SlaDeadline = deadline,
            SlaAlertAt = calculator.CalculateAlertAt(receivedAt, deadline), CreatedByUserId = actorId,
            CreatedAt = now, UpdatedAt = now
        };
        db.Complaints.Add(complaint);
        db.History.Add(NewHistory(complaint.Id, actorId, "RECLAMO_CREADO", null,
            new { complaint.Code, calculated.Score, Priority = priority.Name, SlaHours = calculated.SlaHours, Rules = calculated.MatchedRules }, now));
        await db.SaveChangesAsync(ct);
        return (complaint, []);
    }

    public async Task RefreshOpenPrioritiesAsync(Guid actorId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var complaints = await db.Complaints.Include(x => x.Status).Include(x => x.Category).Include(x => x.Subcategory)
            .Where(x => !x.Status.IsFinal && x.ReceivedAt < now.AddHours(-24)).ToListAsync(ct);
        if (complaints.Count == 0) return;

        foreach (var complaint in complaints)
        {
            var calculated = calculator.Calculate(new PriorityInput(complaint.Category.Name, complaint.Subcategory?.Name,
                complaint.AffectedAmount, complaint.DigitalChannelUnavailable, complaint.ReceivedAt, now));
            if (calculated.Score == complaint.PriorityScore) continue;
            var priority = await FindPriorityAsync(calculated, ct);
            var policy = await FindPolicyAsync(priority.Id, complaint.CategoryId, now, calculated.SlaHours, ct);
            var before = new { complaint.PriorityScore, complaint.PriorityId, complaint.SlaDeadline };
            var deadline = calculator.CalculateDeadline(complaint.ReceivedAt, calculated.SlaHours);
            complaint.PriorityScore = calculated.Score;
            complaint.PriorityId = priority.Id;
            complaint.PriorityBreakdown = JsonSerializer.Serialize(calculated.MatchedRules, JsonOptions);
            complaint.SlaPolicyId = policy.Id;
            complaint.SlaDeadline = deadline;
            complaint.SlaAlertAt = calculator.CalculateAlertAt(complaint.ReceivedAt, deadline);
            complaint.UpdatedAt = now;
            db.History.Add(NewHistory(complaint.Id, actorId, "PRIORIDAD_RECALCULADA", before,
                new { calculated.Score, Priority = priority.Name, SlaHours = calculated.SlaHours, Rules = calculated.MatchedRules }, now));
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangeStatusAsync(Guid id, ChangeStatusRequest request, Guid actorId, CancellationToken ct)
    {
        var complaint = await db.Complaints.Include(x => x.Status).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ResourceNotFoundException("El reclamo no existe.");
        if (complaint.Status.IsFinal) throw new DomainRuleException("Un reclamo finalizado no puede volver a abrirse.");
        if (string.IsNullOrWhiteSpace(request.Observation))
            throw new DomainRuleException("Explica el motivo del cambio de estado.");
        var target = await db.Statuses.SingleOrDefaultAsync(x => x.Id == request.StatusId && x.IsActive, ct)
            ?? throw new DomainRuleException("El estado de destino no existe o está inactivo.");
        if (!await db.StatusTransitions.AnyAsync(x => x.FromStatusId == complaint.StatusId && x.ToStatusId == target.Id, ct))
            throw new DomainRuleException($"No se permite cambiar de {complaint.Status.Name} a {target.Name}.");

        var now = clock.GetUtcNow();
        var previous = complaint.Status.Name;
        complaint.StatusId = target.Id;
        complaint.UpdatedAt = now;
        if (target.Name == "Resuelto") complaint.ResolvedAt = now;
        db.Observations.Add(new ComplaintObservation
        { Id = Guid.NewGuid(), ComplaintId = id, AuthorUserId = actorId, Content = request.Observation.Trim(), CreatedAt = now });
        db.History.Add(NewHistory(id, actorId, "ESTADO_CAMBIADO", new { Estado = previous },
            new { Estado = target.Name, Observacion = request.Observation.Trim() }, now));
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
        complaint.UpdatedAt = now;
        db.Assignments.Add(new ComplaintAssignment { Id = Guid.NewGuid(), ComplaintId = id, AnalystId = request.AnalystId,
            AssignedByUserId = actorId, AssignedAt = now, Reason = request.Reason?.Trim() });
        db.History.Add(NewHistory(id, actorId, previousAssignee is null ? "RECLAMO_ASIGNADO" : "RECLAMO_REASIGNADO",
            new { ResponsableId = previousAssignee }, new { ResponsableId = request.AnalystId }, now));
        await db.SaveChangesAsync(ct);
    }

    public async Task AddObservationAsync(Guid id, AddObservationRequest request, Guid actorId, CancellationToken ct)
    {
        if (!await db.Complaints.AnyAsync(x => x.Id == id, ct)) throw new ResourceNotFoundException("El reclamo no existe.");
        var now = clock.GetUtcNow();
        db.Observations.Add(new ComplaintObservation { Id = Guid.NewGuid(), ComplaintId = id, AuthorUserId = actorId,
            Content = request.Content.Trim(), IsInternal = request.IsInternal, CreatedAt = now });
        db.History.Add(NewHistory(id, actorId, "OBSERVACION_AGREGADA", null, new { request.IsInternal }, now));
        await db.SaveChangesAsync(ct);
    }

    internal static ComplaintListItem MapListItem(Complaint x, DateTimeOffset now, IPriorityAndSlaService calculator) => new(
        x.Id, x.Code, $"{x.Customer.FirstNames} {x.Customer.LastNames}", x.Category.Name, x.Status.Name, x.Priority.Name,
        x.CurrentAssignee is null ? null : $"{x.CurrentAssignee.FirstNames} {x.CurrentAssignee.LastNames}", x.ReceivedAt,
        x.SlaDeadline, calculator.GetSlaState(x.SlaDeadline, x.SlaAlertAt, now));

    public static IReadOnlyList<PriorityRuleMatch> ReadPriorityRules(string json) =>
        JsonSerializer.Deserialize<IReadOnlyList<PriorityRuleMatch>>(json, JsonOptions) ?? [];

    private async Task<PriorityLevel> FindPriorityAsync(PriorityCalculation calculated, CancellationToken ct) =>
        await db.Priorities.SingleOrDefaultAsync(x => x.IsActive && x.Name == calculated.Level, ct)
        ?? throw new DomainRuleException("No existe un nivel de prioridad configurado para el puntaje.");

    private async Task<SlaPolicy> FindPolicyAsync(short priorityId, short categoryId, DateTimeOffset now, int expectedHours, CancellationToken ct)
    {
        var policies = await db.SlaPolicies.AsNoTracking().Where(x => x.PriorityId == priorityId && x.IsActive).ToListAsync(ct);
        var policy = calculator.SelectPolicy(policies, priorityId, categoryId, now);
        if (policy.ResolutionHours != expectedHours)
            throw new DomainRuleException($"La política SLA de {expectedHours} horas no coincide con la prioridad calculada.");
        return policy;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ComplaintHistory NewHistory(Guid complaintId, Guid actorId, string type, object? before, object? after, DateTimeOffset now) => new()
    {
        ComplaintId = complaintId, ActorUserId = actorId, EventType = type,
        BeforeData = before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
        AfterData = after is null ? null : JsonSerializer.Serialize(after, JsonOptions), OccurredAt = now
    };
}
