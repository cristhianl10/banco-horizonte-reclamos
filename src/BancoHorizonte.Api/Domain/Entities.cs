namespace BancoHorizonte.Api.Domain;

public sealed class Role
{
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class AppUser
{
    public Guid Id { get; set; }
    public string FirstNames { get; set; } = string.Empty;
    public string LastNames { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
}

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public short RoleId { get; set; }
    public AppUser User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public sealed class Customer
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentNumber { get; set; } = string.Empty;
    public string FirstNames { get; set; } = string.Empty;
    public string LastNames { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ReceptionChannel
{
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ComplaintCategory
{
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ComplaintSubcategory
{
    public short Id { get; set; }
    public short CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class ComplaintStatus
{
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsFinal { get; set; }
    public short Order { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class StatusTransition
{
    public short FromStatusId { get; set; }
    public short ToStatusId { get; set; }
}

public sealed class PriorityLevel
{
    public short Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public short MinimumScore { get; set; }
    public short Order { get; set; }
    public string ColorHex { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class SlaPolicy
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public short? CategoryId { get; set; }
    public short PriorityId { get; set; }
    public int ResolutionHours { get; set; }
    public int AlertThresholdMinutes { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset? ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Complaint
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public short ReceptionChannelId { get; set; }
    public short CategoryId { get; set; }
    public short? SubcategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? AffectedAmount { get; set; }
    public bool DigitalChannelUnavailable { get; set; }
    public short StatusId { get; set; }
    public short PriorityId { get; set; }
    public short PriorityScore { get; set; }
    public Guid SlaPolicyId { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset SlaAlertAt { get; set; }
    public DateTimeOffset SlaDeadline { get; set; }
    public string PriorityBreakdown { get; set; } = "[]";
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? CurrentAssigneeId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Customer Customer { get; set; } = null!;
    public ReceptionChannel ReceptionChannel { get; set; } = null!;
    public ComplaintCategory Category { get; set; } = null!;
    public ComplaintSubcategory? Subcategory { get; set; }
    public ComplaintStatus Status { get; set; } = null!;
    public PriorityLevel Priority { get; set; } = null!;
    public SlaPolicy SlaPolicy { get; set; } = null!;
    public AppUser? CurrentAssignee { get; set; }
    public ICollection<ComplaintObservation> Observations { get; set; } = [];
    public ICollection<ComplaintHistory> History { get; set; } = [];
}

public sealed class ComplaintAssignment
{
    public Guid Id { get; set; }
    public Guid ComplaintId { get; set; }
    public Guid AnalystId { get; set; }
    public Guid AssignedByUserId { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? Reason { get; set; }
}

public sealed class ComplaintObservation
{
    public Guid Id { get; set; }
    public Guid ComplaintId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public AppUser Author { get; set; } = null!;
}

public sealed class ComplaintHistory
{
    public long Id { get; set; }
    public Guid ComplaintId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BeforeData { get; set; }
    public string? AfterData { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public AppUser Actor { get; set; } = null!;
}
