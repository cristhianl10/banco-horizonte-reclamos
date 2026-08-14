using System.Globalization;
using System.Text;
using BancoHorizonte.Api.Contracts;
using BancoHorizonte.Api.Domain;

namespace BancoHorizonte.Api.Application;

public sealed record PriorityInput(
    string Category,
    string? Subcategory,
    decimal? AffectedAmount,
    bool DigitalChannelUnavailable,
    DateTimeOffset ReceivedAt,
    DateTimeOffset EvaluatedAt,
    bool IsOpen = true);

public interface IPriorityAndSlaService
{
    PriorityCalculation Calculate(PriorityInput input);
    SlaPolicy SelectPolicy(IEnumerable<SlaPolicy> policies, short priorityId, short categoryId, DateTimeOffset now);
    DateTimeOffset CalculateDeadline(DateTimeOffset receivedAt, int resolutionHours);
    DateTimeOffset CalculateAlertAt(DateTimeOffset receivedAt, DateTimeOffset deadline);
    string GetSlaState(DateTimeOffset deadline, DateTimeOffset alertAt, DateTimeOffset now);
}

public sealed class PriorityAndSlaService : IPriorityAndSlaService
{
    public PriorityCalculation Calculate(PriorityInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Category))
            throw new DomainRuleException("La categoría es obligatoria.");
        if (input.AffectedAmount < 0)
            throw new DomainRuleException("El monto afectado no puede ser negativo.");
        if (input.ReceivedAt > input.EvaluatedAt.AddMinutes(5))
            throw new DomainRuleException("La fecha de recepción no puede estar en el futuro.");

        var text = Normalize($"{input.Category} {input.Subcategory}");
        var rules = new List<PriorityRuleMatch>();

        if (ContainsAny(text, "transaccion no reconocida", "compra no reconocida", "consumo no reconocido", "debito no autorizado"))
            rules.Add(new("Transacción o compra no reconocida", 4));

        var transferNotCredited = text.Contains("transferencia") && ContainsAny(text, "no acreditada", "no recibida", "no reflejada");
        var digitalAccessBlocked = text.Contains("canales digitales") && ContainsAny(text, "acceso bloqueado", "canal bloqueado", "no puede iniciar sesion");
        if (transferNotCredited || digitalAccessBlocked)
            rules.Add(new("Transferencia no acreditada o acceso/canal bloqueado", 3));

        if (input.AffectedAmount >= 500m)
            rules.Add(new("Monto afectado igual o superior a USD 500", 3));

        if (input.DigitalChannelUnavailable)
            rules.Add(new("Canal digital completamente indisponible", 2));

        if (input.IsOpen && input.EvaluatedAt - input.ReceivedAt > TimeSpan.FromHours(24))
            rules.Add(new("Reclamo abierto por más de 24 horas", 2));

        var score = (short)rules.Sum(x => x.Points);
        var (level, hours) = score switch
        {
            >= 7 => ("Crítica", 2),
            >= 5 => ("Alta", 6),
            >= 3 => ("Media", 12),
            _ => ("Baja", 24)
        };
        return new PriorityCalculation(score, level, hours, rules);
    }

    public SlaPolicy SelectPolicy(IEnumerable<SlaPolicy> policies, short priorityId, short categoryId, DateTimeOffset now)
    {
        var candidates = policies.Where(x => x.IsActive && x.PriorityId == priorityId &&
            x.ValidFrom <= now && (x.ValidUntil == null || x.ValidUntil > now)).ToList();
        return candidates.FirstOrDefault(x => x.CategoryId == categoryId)
            ?? candidates.FirstOrDefault(x => x.CategoryId == null)
            ?? throw new DomainRuleException("No existe una política SLA vigente para la prioridad calculada.");
    }

    public DateTimeOffset CalculateDeadline(DateTimeOffset receivedAt, int resolutionHours)
    {
        if (resolutionHours <= 0) throw new DomainRuleException("Las horas del SLA deben ser mayores a cero.");
        return receivedAt.AddHours(resolutionHours);
    }

    public DateTimeOffset CalculateAlertAt(DateTimeOffset receivedAt, DateTimeOffset deadline)
    {
        if (deadline <= receivedAt) throw new DomainRuleException("La fecha límite debe ser posterior a la recepción.");
        return receivedAt.AddTicks((deadline - receivedAt).Ticks * 3 / 4);
    }

    public string GetSlaState(DateTimeOffset deadline, DateTimeOffset alertAt, DateTimeOffset now)
    {
        if (alertAt >= deadline) throw new DomainRuleException("La alerta SLA debe ser anterior a la fecha límite.");
        if (deadline <= now) return "Vencido";
        return alertAt <= now ? "Próximo" : "En tiempo";
    }

    private static bool ContainsAny(string source, params string[] values) => values.Any(source.Contains);

    private static string Normalize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var withoutMarks = new string(normalized.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark).ToArray());
        return withoutMarks.Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
