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
    public async Task Create_NormalizesCustomerAndCalculatesPriorityAndSla()
    {
        await using var db = CreateDb(); await SeedAsync(db);
        var service = CreateService(db);
        var result = await service.CreateAsync(ValidRequest(), ActorId, default);

        Assert.NotNull(result.Complaint);
        Assert.StartsWith("BH-20260811-", result.Complaint.Code);
        Assert.Equal(9, result.Complaint.PriorityScore);
        Assert.Equal(Now.AddHours(4), result.Complaint.SlaDeadline);
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
    public async Task Create_AllowsConfirmedDuplicateAndAddsRepeatPenalty()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db);
        await service.CreateAsync(ValidRequest(), ActorId, default);
        var result = await service.CreateAsync(ValidRequest() with { ConfirmPossibleDuplicate = true }, ActorId, default);
        Assert.NotNull(result.Complaint);
        Assert.Equal(11, result.Complaint.PriorityScore);
        Assert.Equal(2, await db.Complaints.CountAsync());
    }

    [Fact]
    public async Task Create_RejectsSubcategoryFromAnotherCategory()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db);
        await Assert.ThrowsAsync<DomainRuleException>(() => service.CreateAsync(ValidRequest() with { SubcategoryId = 2 }, ActorId, default));
    }

    [Fact]
    public async Task Create_RejectsInactiveChannel()
    {
        await using var db = CreateDb(); await SeedAsync(db); db.ReceptionChannels.Single().IsActive = false; await db.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainRuleException>(() => CreateService(db).CreateAsync(ValidRequest(), ActorId, default));
    }

    [Fact]
    public async Task ChangeStatus_PersistsValidTransitionObservationAndHistory()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db);
        var created = (await service.CreateAsync(ValidRequest(), ActorId, default)).Complaint!;
        await service.ChangeStatusAsync(created.Id, new ChangeStatusRequest(2, "Caso validado"), ActorId, default);
        Assert.Equal(2, created.StatusId);
        Assert.Contains(db.Observations, x => x.Content == "Caso validado");
        Assert.Contains(db.History, x => x.EventType == "ESTADO_CAMBIADO");
    }

    [Fact]
    public async Task ChangeStatus_RejectsTransitionNotConfigured()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db);
        var created = (await service.CreateAsync(ValidRequest(), ActorId, default)).Complaint!;
        await Assert.ThrowsAsync<DomainRuleException>(() => service.ChangeStatusAsync(created.Id, new ChangeStatusRequest(5, null), ActorId, default));
    }

    [Fact]
    public async Task ChangeStatus_RejectsFinalComplaint()
    {
        await using var db = CreateDb(); await SeedAsync(db); var complaint = SeedComplaint(db, statusId: 6); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainRuleException>(() => CreateService(db).ChangeStatusAsync(complaint.Id, new ChangeStatusRequest(3, null), ActorId, default));
    }

    [Fact]
    public async Task Assign_CreatesSingleActiveAssignmentAndReassigns()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db);
        var complaint = SeedComplaint(db); await db.SaveChangesAsync();
        await service.AssignAsync(complaint.Id, new AssignComplaintRequest(AnalystId, "Carga inicial"), ActorId, default);
        var secondAnalyst = AddUser(db, Guid.NewGuid(), "Segundo", "Analista", "segundo@demo.com", "Analista"); await db.SaveChangesAsync();
        await service.AssignAsync(complaint.Id, new AssignComplaintRequest(secondAnalyst.Id, "Balance de carga"), ActorId, default);
        Assert.Equal(secondAnalyst.Id, complaint.CurrentAssigneeId);
        Assert.Single(db.Assignments.Where(x => x.EndedAt == null));
        Assert.Single(db.Assignments.Where(x => x.EndedAt != null));
    }

    [Fact]
    public async Task Assign_RejectsOperatorAsAssignee()
    {
        await using var db = CreateDb(); await SeedAsync(db); var complaint = SeedComplaint(db); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainRuleException>(() => CreateService(db).AssignAsync(complaint.Id, new AssignComplaintRequest(ActorId, null), ActorId, default));
    }

    [Fact]
    public async Task Assign_RejectsSameActiveAssignee()
    {
        await using var db = CreateDb(); await SeedAsync(db); var service = CreateService(db); var complaint = SeedComplaint(db); await db.SaveChangesAsync();
        await service.AssignAsync(complaint.Id, new AssignComplaintRequest(AnalystId, null), ActorId, default);
        await Assert.ThrowsAsync<DomainRuleException>(() => service.AssignAsync(complaint.Id, new AssignComplaintRequest(AnalystId, null), ActorId, default));
    }

    [Fact]
    public async Task AddObservation_RejectsUnknownComplaint()
    {
        await using var db = CreateDb(); await SeedAsync(db);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => CreateService(db).AddObservationAsync(Guid.NewGuid(), new AddObservationRequest("Comentario válido"), ActorId, default));
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static ComplaintService CreateService(AppDbContext db) => new(db, new PriorityAndSlaService(), new FixedTimeProvider(Now));

    private static async Task SeedAsync(AppDbContext db)
    {
        db.Roles.AddRange(new Role { Id = 1, Name = "Operador", Description = "" }, new Role { Id = 2, Name = "Analista", Description = "" });
        AddUser(db, ActorId, "Actor", "Operador", "actor@demo.com", "Operador");
        AddUser(db, AnalystId, "Ana", "Analista", "analista@demo.com", "Analista");
        db.ReceptionChannels.Add(new ReceptionChannel { Id = 1, Name = "Llamada" });
        db.Categories.AddRange(new ComplaintCategory { Id = 1, Name = "Transferencias" }, new ComplaintCategory { Id = 2, Name = "Tarjetas" });
        db.Subcategories.AddRange(new ComplaintSubcategory { Id = 1, CategoryId = 1, Name = "No recibida" }, new ComplaintSubcategory { Id = 2, CategoryId = 2, Name = "Bloqueada" });
        db.Statuses.AddRange(new ComplaintStatus { Id = 1, Name = "Nuevo", Order = 1 }, new ComplaintStatus { Id = 2, Name = "Asignado", Order = 2 }, new ComplaintStatus { Id = 3, Name = "En análisis", Order = 3 }, new ComplaintStatus { Id = 5, Name = "Resuelto", Order = 5 }, new ComplaintStatus { Id = 6, Name = "Cerrado", IsFinal = true, Order = 6 });
        db.StatusTransitions.AddRange(new StatusTransition { FromStatusId = 1, ToStatusId = 2 }, new StatusTransition { FromStatusId = 2, ToStatusId = 3 }, new StatusTransition { FromStatusId = 3, ToStatusId = 5 });
        db.Priorities.AddRange(new PriorityLevel { Id = 1, Name = "Baja", MinimumScore = 0, Order = 1, ColorHex = "#000000" }, new PriorityLevel { Id = 2, Name = "Media", MinimumScore = 3, Order = 2, ColorHex = "#000000" }, new PriorityLevel { Id = 3, Name = "Alta", MinimumScore = 5, Order = 3, ColorHex = "#000000" }, new PriorityLevel { Id = 4, Name = "Crítica", MinimumScore = 7, Order = 4, ColorHex = "#000000" });
        db.SlaPolicies.AddRange(new SlaPolicy { Id = Guid.NewGuid(), Name = "Baja", PriorityId = 1, ResolutionHours = 72, AlertThresholdMinutes = 720, ValidFrom = Now.AddDays(-1) }, new SlaPolicy { Id = Guid.NewGuid(), Name = "Media", PriorityId = 2, ResolutionHours = 24, AlertThresholdMinutes = 240, ValidFrom = Now.AddDays(-1) }, new SlaPolicy { Id = Guid.NewGuid(), Name = "Alta", PriorityId = 3, ResolutionHours = 8, AlertThresholdMinutes = 120, ValidFrom = Now.AddDays(-1) }, new SlaPolicy { Id = Guid.NewGuid(), Name = "Crítica", PriorityId = 4, ResolutionHours = 4, AlertThresholdMinutes = 60, ValidFrom = Now.AddDays(-1) });
        await db.SaveChangesAsync();
    }

    private static AppUser AddUser(AppDbContext db, Guid id, string first, string last, string email, string role)
    {
        var roleEntity = db.Roles.Local.Single(x => x.Name == role);
        var user = new AppUser { Id = id, FirstNames = first, LastNames = last, Email = email, CreatedAt = Now, UpdatedAt = Now };
        user.UserRoles.Add(new UserRole { UserId = id, RoleId = roleEntity.Id, User = user, Role = roleEntity }); db.Users.Add(user); return user;
    }

    private static CreateComplaintRequest ValidRequest() => new(new CustomerRequest(" CEDULA ", " 0912345678 ", " Ana ", " Vega ", "ANA@EXAMPLE.COM", "0990000000"), 1, 1, 1, "Transferencia debitada pero no recibida por el beneficiario.", 3, 3);

    private static Complaint SeedComplaint(AppDbContext db, short statusId = 1)
    {
        var complaint = new Complaint { Id = Guid.NewGuid(), Code = $"BH-{Guid.NewGuid():N}", CustomerId = Guid.NewGuid(), ReceptionChannelId = 1, CategoryId = 1, Description = "Descripción suficientemente extensa", Impact = 2, Urgency = 2, StatusId = statusId, PriorityId = 2, PriorityScore = 4, SlaPolicyId = db.SlaPolicies.Local.Single(x => x.PriorityId == 2).Id, ReceivedAt = Now, SlaDeadline = Now.AddHours(24), CreatedByUserId = ActorId, CreatedAt = Now, UpdatedAt = Now };
        db.Customers.Add(new Customer { Id = complaint.CustomerId, DocumentType = "CEDULA", DocumentNumber = Guid.NewGuid().ToString("N"), FirstNames = "Cliente", LastNames = "Demo", CreatedAt = Now, UpdatedAt = Now }); db.Complaints.Add(complaint); return complaint;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
}
