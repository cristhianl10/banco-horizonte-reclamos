using System.ComponentModel.DataAnnotations;

namespace BancoHorizonte.Api.Contracts;

public sealed record CustomerRequest(
    [property: Required, StringLength(20)] string DocumentType,
    [property: Required, StringLength(30, MinimumLength = 5)] string DocumentNumber,
    [property: Required, StringLength(100, MinimumLength = 2)] string FirstNames,
    [property: Required, StringLength(100, MinimumLength = 2)] string LastNames,
    [property: EmailAddress, StringLength(254)] string? Email,
    [property: Phone, StringLength(30)] string? Phone);

public sealed record CreateComplaintRequest(
    [property: Required] CustomerRequest Customer,
    [property: Range(1, short.MaxValue)] short ReceptionChannelId,
    [property: Range(1, short.MaxValue)] short CategoryId,
    short? SubcategoryId,
    [property: Required, StringLength(4000, MinimumLength = 20)] string Description,
    [property: Range(1, 3)] short Impact,
    [property: Range(1, 3)] short Urgency,
    bool ConfirmPossibleDuplicate = false);

public sealed record ChangeStatusRequest(
    [property: Range(1, short.MaxValue)] short StatusId,
    [property: StringLength(1000)] string? Observation);

public sealed record AssignComplaintRequest(
    [property: Required] Guid AnalystId,
    [property: StringLength(500)] string? Reason);

public sealed record AddObservationRequest(
    [property: Required, StringLength(2000, MinimumLength = 3)] string Content,
    bool IsInternal = true);

public sealed record ComplaintQuery(
    int Page = 1,
    int PageSize = 20,
    short? StatusId = null,
    short? PriorityId = null,
    short? CategoryId = null,
    Guid? AssigneeId = null,
    string? Search = null,
    string? Sla = null);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public sealed record ComplaintListItem(
    Guid Id, string Code, string Customer, string Category, string Status, string Priority,
    string? Assignee, DateTimeOffset ReceivedAt, DateTimeOffset SlaDeadline, string SlaState);

public sealed record TimelineItem(string Type, string Description, string Actor, DateTimeOffset At);

public sealed record ComplaintDetail(
    Guid Id, string Code, string Customer, string Document, string? Email, string? Phone,
    string Channel, string Category, string? Subcategory, string Description, short Impact,
    short Urgency, string Status, short StatusId, string Priority, short PriorityScore,
    string? Assignee, Guid? AssigneeId, DateTimeOffset ReceivedAt, DateTimeOffset SlaDeadline,
    string SlaState, IReadOnlyList<TimelineItem> Timeline);

public sealed record CatalogItem(short Id, string Name);
public sealed record SubcategoryItem(short Id, string Name, short CategoryId);
public sealed record AnalystItem(Guid Id, string Name, string Email);
public sealed record CatalogResponse(
    IReadOnlyList<CatalogItem> Channels,
    IReadOnlyList<CatalogItem> Categories,
    IReadOnlyList<SubcategoryItem> Subcategories,
    IReadOnlyList<CatalogItem> Statuses,
    IReadOnlyList<CatalogItem> Priorities,
    IReadOnlyList<AnalystItem> Analysts);

public sealed record DashboardSummary(
    int Open, int Critical, int NearDeadline, int Overdue, decimal SlaCompliance,
    IReadOnlyList<MetricSlice> ByStatus, IReadOnlyList<MetricSlice> ByCategory,
    IReadOnlyList<AnalystLoad> AnalystLoads, IReadOnlyList<ComplaintListItem> ImmediateAttention);

public sealed record MetricSlice(string Label, int Value);
public sealed record AnalystLoad(string Analyst, int ActiveCases);
public sealed record CurrentUserResponse(Guid Id, string Name, string Email, IReadOnlyList<string> Roles);

public sealed record PriorityCalculation(short Score, string Level);

public sealed class DomainRuleException(string message) : Exception(message);
public sealed class ResourceNotFoundException(string message) : Exception(message);
