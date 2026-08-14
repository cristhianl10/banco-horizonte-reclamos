using BancoHorizonte.Api.Application;
using BancoHorizonte.Api.Contracts;
using BancoHorizonte.Api.Domain;
using BancoHorizonte.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BancoHorizonte.Api.Tests;

public sealed class ComplaintServiceTests
{
    private static readonly Guid ActorId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AnalystId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-11T12:00:00Z");

    [Fact]
    public async Task Create_NormalizesCustomerAndCalculatesCriticalCase()
    {
        await using var db = CreateDb(); await SeedAsync(db);
        var result = await CreateService(db).CreateAsync(ValidRequest(), ActorId, default);

        Assert.NotNull(result.Complaint);
        Assert.StartsWith("BH-20260811-", result.Complaint.Code);
        Assert.Equal(7, result.Complaint.PriorityScore);
        Assert.Equal(Now.AddHours(2), result.Complaint.SlaDeadline);
        Assert.Equal(Now.AddMinutes(90), result.Complaint.SlaAlertAt);
        Assert.Equal(2, ComplaintService.ReadPriorityRules(result.Complaint.PriorityBreakdown).Count);
        var customer = await db.Customers.SingleAsync();
        Assert.Equal("0912345678", customer.DocumentNumber);
        Assert.Equal("ana@example.com", customer.Email);
        Assert.Single(db.History);
    }

    [Fact]
    public async Task Create_ReturnsPossibleDuplicateBeforePersistingSecondCase()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db);
        await service.CreateAsync(ValidRequest(), ActorId, default);
        var result = await service.CreateAsync(ValidRequest(), ActorId, default);
        Assert.Null(result.Complaint);
        Assert.Single(result.Duplicates);
        Assert.Single(db.Complaints);
    }

    [Fact]
    public async Task Create_AllowsConfirmedDuplicateWithoutInventedRepeatPenalty()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db);
        await service.CreateAsync(ValidRequest(), ActorId, default);
        var result = await service.CreateAsync(ValidRequest() with { ConfirmPossibleDuplicate = true }, ActorId, default);
        Assert.NotNull(result.Complaint);
        Assert.Equal(7, result.Complaint.PriorityScore);
        Assert.Equal(2, await db.Complaints.CountAsync());
    }

    [Fact]
    public async Task Create_RejectsSubcategoryFromAnotherCategory()
    {
        await using var db = CreateDb(); await SeedAsync(db);
        await Assert.ThrowsAsync<DomainRuleException>(() => CreateService(db).CreateAsync(ValidRequest() with { SubcategoryId = 1 }, ActorId, default));
    }

    [Fact]
    public async Task Create_RejectsFutureReception()
    {
        await using var db = CreateDb(); await SeedAsync(db);
        await Assert.ThrowsAsync<DomainRuleException>(() => CreateService(db).CreateAsync(ValidRequest() with { ReceivedAt = Now.AddHours(1) }, ActorId, default));
    }

    [Fact]
    public async Task RefreshOpenPriorities_AddsAgeRuleOnce()
    {
        await using var db = CreateDb(); await SeedAsync(db);
        var complaint = SeedComplaint(db, receivedAt: Now.AddHours(-25)); await db.SaveChangesAsync();
        var service = CreateService(db);
        await service.RefreshOpenPrioritiesAsync(ActorId, default);
        await service.RefreshOpenPrioritiesAsync(ActorId, default);
        Assert.Equal(2, complaint.PriorityScore);
        Assert.Single(db.History.Where(x => x.EventType == "PRIORIDAD_RECALCULADA"));
    }

    [Fact]
    public async Task ChangeStatus_PersistsRequiredObservationAndHistory()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db);
        var created = (await service.CreateAsync(ValidRequest(), ActorId, default)).Complaint!;
        await service.ChangeStatusAsync(created.Id, new ChangeStatusRequest(2, "Caso validado"), ActorId, default);
        Assert.Equal(2, created.StatusId);
        Assert.Contains(db.Observations, x => x.Content == "Caso validado");
        Assert.Contains(db.History, x => x.EventType == "ESTADO_CAMBIADO");
    }

    [Fact]
    public async Task ChangeStatus_RejectsEmptyObservation()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db);
        var created = (await service.CreateAsync(ValidRequest(), ActorId, default)).Complaint!;
        await Assert.ThrowsAsync<DomainRuleException>(() => service.ChangeStatusAsync(created.Id, new ChangeStatusRequest(2, " "), ActorId, default));
    }

    [Fact]
    public async Task ChangeStatus_RejectsInvalidTransitionAndFinalReopening()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db);
        var created = (await service.CreateAsync(ValidRequest(), ActorId, default)).Complaint!;
        await Assert.ThrowsAsync<DomainRuleException>(() => service.ChangeStatusAsync(created.Id, new ChangeStatusRequest(3, "Salto inválido"), ActorId, default));
        var final = SeedComplaint(db, statusId: 3); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainRuleException>(() => service.ChangeStatusAsync(final.Id, new ChangeStatusRequest(2, "Reabrir"), ActorId, default));
    }

    [Fact]
    public async Task Assign_CreatesSingleActiveAssignmentWithoutChangingStatus()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db);
        var complaint = SeedComplaint(db); await db.SaveChangesAsync();
        await service.AssignAsync(complaint.Id, new AssignComplaintRequest(AnalystId, "Carga inicial"), ActorId, default);
        var second = AddUser(db, Guid.NewGuid(), "Segundo", "Analista", "segundo@demo.com", "Analista"); await db.SaveChangesAsync();
        await service.AssignAsync(complaint.Id, new AssignComplaintRequest(second.Id, "Balance de carga"), ActorId, default);
        Assert.Equal(second.Id, complaint.CurrentAssigneeId);
        Assert.Equal(1, complaint.StatusId);
        Assert.Single(db.Assignments.Where(x => x.EndedAt == null));
        Assert.Single(db.Assignments.Where(x => x.EndedAt != null));
    }

    [Fact]
    public async Task Assign_RejectsOperatorAsAssignee()
    {
        await using var db = CreateDb(); await SeedAsync(db); var complaint = SeedComplaint(db); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainRuleException>(() => CreateService(db).AssignAsync(complaint.Id,
            new AssignComplaintRequest(ActorId, null), ActorId, default));
    }

    [Fact]
    public async Task AddObservation_RejectsUnknownComplaint()
    {
        await using var db = CreateDb(); await SeedAsync(db);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => CreateService(db).AddObservationAsync(Guid.NewGuid(),
            new AddObservationRequest("Comentario válido"), ActorId, default));
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ComplaintService CreateService(AppDbContext db) => new(db, new PriorityAndSlaService(), new FixedTimeProvider(Now));

    private static async Task SeedAsync(AppDbContext db)
    {
        db.Roles.AddRange(new Role { Id = 1, Name = "Operador", Description = "" }, new Role { Id = 2, Name = "Analista", Description = "" });
        AddUser(db, ActorId, "Actor", "Operador", "actor@demo.com", "Operador");
        AddUser(db, AnalystId, "Ana", "Analista", "analista@demo.com", "Analista");
        db.ReceptionChannels.Add(new ReceptionChannel { Id = 1, Name = "Llamada" });
        db.Categories.AddRange(new ComplaintCategory { Id = 1, Name = "Transferencias" }, new ComplaintCategory { Id = 2, Name = "Tarjetas" });
        db.Subcategories.AddRange(new ComplaintSubcategory { Id = 1, CategoryId = 1, Name = "Transferencia no acreditada" },
            new ComplaintSubcategory { Id = 2, CategoryId = 2, Name = "Compra no reconocida" });
        db.Statuses.AddRange(new ComplaintStatus { Id = 1, Name = "Nuevo", Order = 1 },
            new ComplaintStatus { Id = 2, Name = "En análisis", Order = 2 },
            new ComplaintStatus { Id = 3, Name = "Resuelto", IsFinal = true, Order = 3 },
            new ComplaintStatus { Id = 4, Name = "Rechazado", IsFinal = true, Order = 4 });
        db.StatusTransitions.AddRange(new StatusTransition { FromStatusId = 1, ToStatusId = 2 }, new StatusTransition { FromStatusId = 1, ToStatusId = 4 },
            new StatusTransition { FromStatusId = 2, ToStatusId = 3 }, new StatusTransition { FromStatusId = 2, ToStatusId = 4 });
        db.Priorities.AddRange(new PriorityLevel { Id = 1, Name = "Baja", MinimumScore = 0, Order = 1, ColorHex = "#000000" },
            new PriorityLevel { Id = 2, Name = "Media", MinimumScore = 3, Order = 2, ColorHex = "#000000" },
            new PriorityLevel { Id = 3, Name = "Alta", MinimumScore = 5, Order = 3, ColorHex = "#000000" },
            new PriorityLevel { Id = 4, Name = "Crítica", MinimumScore = 7, Order = 4, ColorHex = "#000000" });
        db.SlaPolicies.AddRange(Policy(1, 24), Policy(2, 12), Policy(3, 6), Policy(4, 2));
        await db.SaveChangesAsync();
    }

    private static SlaPolicy Policy(short priority, int hours) => new() { Id = Guid.NewGuid(), Name = $"SLA {priority}",
        PriorityId = priority, ResolutionHours = hours, AlertThresholdMinutes = hours * 45, ValidFrom = Now.AddDays(-1) };

    private static AppUser AddUser(AppDbContext db, Guid id, string first, string last, string email, string role)
    {
        var roleEntity = db.Roles.Local.Single(x => x.Name == role);
        var user = new AppUser { Id = id, FirstNames = first, LastNames = last, Email = email, CreatedAt = Now, UpdatedAt = Now };
        user.UserRoles.Add(new UserRole { UserId = id, RoleId = roleEntity.Id, User = user, Role = roleEntity });
        db.Users.Add(user); return user;
    }

    private static CreateComplaintRequest ValidRequest() => new(
        new CustomerRequest(" CEDULA ", " 0912345678 ", " Ana ", " Vega ", "ANA@EXAMPLE.COM", "0990000000"),
        1, 2, 2, "Compra no reconocida reportada por la cliente desde su cuenta.", 780, false, Now);

    private static Complaint SeedComplaint(AppDbContext db, short statusId = 1, DateTimeOffset? receivedAt = null)
    {
        var received = receivedAt ?? Now;
        var complaint = new Complaint { Id = Guid.NewGuid(), Code = $"BH-{Guid.NewGuid():N}", CustomerId = Guid.NewGuid(),
            ReceptionChannelId = 1, CategoryId = 1, Description = "Descripción suficientemente extensa", StatusId = statusId,
            PriorityId = 1, PriorityScore = 0, PriorityBreakdown = "[]", SlaPolicyId = db.SlaPolicies.Local.Single(x => x.PriorityId == 1).Id,
            ReceivedAt = received, SlaAlertAt = received.AddHours(18), SlaDeadline = received.AddHours(24), CreatedByUserId = ActorId,
            CreatedAt = Now, UpdatedAt = Now };
        db.Customers.Add(new Customer { Id = complaint.CustomerId, DocumentType = "CEDULA", DocumentNumber = Random.Shared.NextInt64().ToString(),
            FirstNames = "Cliente", LastNames = "Demo", CreatedAt = Now, UpdatedAt = Now });
        db.Complaints.Add(complaint); return complaint;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
