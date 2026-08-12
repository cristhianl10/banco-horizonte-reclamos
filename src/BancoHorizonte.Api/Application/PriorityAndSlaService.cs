using BancoHorizonte.Api.Contracts;
using BancoHorizonte.Api.Domain;

namespace BancoHorizonte.Api.Application;

public interface IPriorityAndSlaService
{
    PriorityCalculation Calculate(short impact, short urgency, string category, bool isRepeatCase);
    SlaPolicy SelectPolicy(IEnumerable<SlaPolicy> policies, short priorityId, short categoryId, DateTimeOffset now);
    DateTimeOffset CalculateDeadline(DateTimeOffset receivedAt, int resolutionHours);
    string GetSlaState(DateTimeOffset deadline, DateTimeOffset now, int alertThresholdMinutes = 240);
}

public sealed class PriorityAndSlaService : IPriorityAndSlaService
{
    private static readonly string[] SensitiveCategories = ["transfer", "tarjeta", "cobro"];

    public PriorityCalculation Calculate(short impact, short urgency, string category, bool isRepeatCase)
    {
        if (impact is < 1 or > 3) throw new DomainRuleException("El impacto debe estar entre 1 y 3.");
        if (urgency is < 1 or > 3) throw new DomainRuleException("La urgencia debe estar entre 1 y 3.");
        if (string.IsNullOrWhiteSpace(category)) throw new DomainRuleException("La categoría es obligatoria.");

        var score = (short)(((impact - 1) * 2) + ((urgency - 1) * 2));
        if (SensitiveCategories.Any(x => category.Contains(x, StringComparison.OrdinalIgnoreCase))) score++;
        if (isRepeatCase) score += 2;

        var level = score switch { >= 7 => "Crítica", >= 5 => "Alta", >= 3 => "Media", _ => "Baja" };
        return new PriorityCalculation(score, level);
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

    public string GetSlaState(DateTimeOffset deadline, DateTimeOffset now, int alertThresholdMinutes = 240)
    {
        if (alertThresholdMinutes <= 0) throw new DomainRuleException("El umbral de alerta debe ser mayor a cero.");
        if (deadline <= now) return "Vencido";
        return deadline <= now.AddMinutes(alertThresholdMinutes) ? "Próximo" : "En tiempo";
    }
}
