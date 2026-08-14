using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BancoHorizonte.Api.Contracts;

public sealed record CustomerRequest(
    [param: Required(ErrorMessage = "Selecciona un tipo de documento."), StringLength(20)] string DocumentType,
    [param: Required(ErrorMessage = "Ingresa el número de documento."), StringLength(20, MinimumLength = 5)] string DocumentNumber,
    [param: Required, StringLength(100, MinimumLength = 2)] string FirstNames,
    [param: Required, StringLength(100, MinimumLength = 2)] string LastNames,
    [param: EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido."), StringLength(254)] string? Email,
    [param: RegularExpression(@"^\d{10}$", ErrorMessage = "El teléfono debe tener exactamente 10 dígitos."), StringLength(10)] string? Phone)
    : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var type = RemoveDiacritics(DocumentType).Trim().ToUpperInvariant();
        var number = DocumentNumber.Trim();

        switch (type)
        {
            case "CEDULA" when !Regex.IsMatch(number, @"^\d{10}$"):
                yield return new ValidationResult(
                    "La cédula debe tener exactamente 10 dígitos.", [nameof(DocumentNumber)]);
                break;
            case "RUC" when !Regex.IsMatch(number, @"^\d{13}$"):
                yield return new ValidationResult(
                    "El RUC debe tener exactamente 13 dígitos.", [nameof(DocumentNumber)]);
                break;
            case "PASAPORTE" when !Regex.IsMatch(number, @"^[A-Za-z0-9]{5,20}$"):
                yield return new ValidationResult(
                    "El pasaporte debe tener entre 5 y 20 caracteres, solo letras y números.", [nameof(DocumentNumber)]);
                break;
            case not ("CEDULA" or "RUC" or "PASAPORTE"):
                yield return new ValidationResult(
                    "Selecciona Cédula, RUC o Pasaporte como tipo de documento.", [nameof(DocumentType)]);
                break;
        }
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        return new string(normalized.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray());
    }
}

public sealed record CreateComplaintRequest(
    [param: Required] CustomerRequest Customer,
    [param: Range(1, short.MaxValue)] short ReceptionChannelId,
    [param: Range(1, short.MaxValue)] short CategoryId,
    short? SubcategoryId,
    [param: Required, StringLength(4000, MinimumLength = 20)] string Description,
    [param: Range(typeof(decimal), "0", "9999999999.99")] decimal? AffectedAmount,
    bool DigitalChannelUnavailable,
    DateTimeOffset? ReceivedAt,
    bool ConfirmPossibleDuplicate = false);

public sealed record ChangeStatusRequest(
    [param: Range(1, short.MaxValue)] short StatusId,
    [param: Required, StringLength(1000, MinimumLength = 3)] string Observation);

public sealed record AssignComplaintRequest(
    [param: Required] Guid AnalystId,
    [param: StringLength(500)] string? Reason);

public sealed record AddObservationRequest(
    [param: Required, StringLength(2000, MinimumLength = 3)] string Content,
    bool IsInternal = true);

public sealed record ComplaintQuery(
    int Page = 1,
    int PageSize = 20,
    short? StatusId = null,
    short? PriorityId = null,
    short? CategoryId = null,
    short? ChannelId = null,
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
    string Channel, string Category, string? Subcategory, string Description, decimal? AffectedAmount,
    bool DigitalChannelUnavailable, string Status, short StatusId, string Priority, short PriorityScore,
    string? Assignee, Guid? AssigneeId, DateTimeOffset ReceivedAt, DateTimeOffset SlaDeadline,
    string SlaState, IReadOnlyList<PriorityRuleMatch> PriorityRules, IReadOnlyList<TimelineItem> Timeline);

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
    int Total, int Open, int Resolved, int Critical, int NearDeadline, int Overdue, decimal SlaCompliance,
    IReadOnlyList<MetricSlice> ByStatus, IReadOnlyList<MetricSlice> ByPriority, IReadOnlyList<MetricSlice> ByCategory,
    IReadOnlyList<AnalystLoad> AnalystLoads, IReadOnlyList<ComplaintListItem> ImmediateAttention);

public sealed record MetricSlice(string Label, int Value);
public sealed record AnalystLoad(string Analyst, int ActiveCases);
public sealed record CurrentUserResponse(Guid Id, string Name, string Email, IReadOnlyList<string> Roles);
public sealed record EmailRegistrationStatusRequest(
    [param: Required, EmailAddress, StringLength(254)] string Email);
public sealed record EmailRegistrationStatusResponse(bool Registered);

public sealed record PriorityRuleMatch(string Rule, short Points);
public sealed record PriorityCalculation(short Score, string Level, int SlaHours, IReadOnlyList<PriorityRuleMatch> MatchedRules);

public sealed class DomainRuleException(string message) : Exception(message);
public sealed class ResourceNotFoundException(string message) : Exception(message);
