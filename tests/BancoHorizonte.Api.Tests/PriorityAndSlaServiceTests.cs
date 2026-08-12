using BancoHorizonte.Api.Application;
using BancoHorizonte.Api.Contracts;
using BancoHorizonte.Api.Domain;

namespace BancoHorizonte.Api.Tests;

public sealed class PriorityAndSlaServiceTests
{
    private readonly PriorityAndSlaService _service = new();

    [Theory]
    [InlineData(1, 1, "Atención al cliente", false, 0, "Baja")]
    [InlineData(2, 2, "Canales digitales", false, 4, "Media")]
    [InlineData(3, 2, "Atención al cliente", false, 6, "Alta")]
    [InlineData(3, 3, "Transferencias", false, 9, "Crítica")]
    [InlineData(2, 2, "Tarjetas", true, 7, "Crítica")]
    [InlineData(1, 2, "Cobros", false, 3, "Media")]
    public void Calculate_ReturnsExpectedScoreAndLevel(short impact, short urgency, string category, bool repeat, short score, string level)
    {
        var result = _service.Calculate(impact, urgency, category, repeat);
        Assert.Equal(score, result.Score);
        Assert.Equal(level, result.Level);
    }

    [Theory]
    [InlineData(0, 1)] [InlineData(4, 1)] [InlineData(1, 0)] [InlineData(1, 4)]
    public void Calculate_RejectsOutOfRangeValues(short impact, short urgency) =>
        Assert.Throws<DomainRuleException>(() => _service.Calculate(impact, urgency, "Cobros", false));

    [Theory] [InlineData("")] [InlineData("   ")]
    public void Calculate_RejectsEmptyCategory(string category) =>
        Assert.Throws<DomainRuleException>(() => _service.Calculate(1, 1, category, false));

    [Fact]
    public void SelectPolicy_PrefersCategorySpecificPolicy()
    {
        var now = DateTimeOffset.Parse("2026-08-11T12:00:00Z");
        var generic = Policy(null, 4, now.AddDays(-1));
        var specific = Policy(2, 4, now.AddDays(-1));
        Assert.Same(specific, _service.SelectPolicy([generic, specific], 4, 2, now));
    }

    [Fact]
    public void SelectPolicy_FallsBackToGenericPolicy()
    {
        var now = DateTimeOffset.UtcNow;
        var generic = Policy(null, 3, now.AddDays(-1));
        Assert.Same(generic, _service.SelectPolicy([generic], 3, 9, now));
    }

    [Fact]
    public void SelectPolicy_RejectsExpiredInactiveAndFuturePolicies()
    {
        var now = DateTimeOffset.UtcNow;
        var policies = new[] { Policy(null, 1, now.AddDays(-2), now.AddDays(-1)), Policy(null, 1, now.AddDays(1)), Policy(null, 1, now.AddDays(-1), active: false) };
        Assert.Throws<DomainRuleException>(() => _service.SelectPolicy(policies, 1, 1, now));
    }

    [Fact]
    public void CalculateDeadline_AddsResolutionHours()
    {
        var received = DateTimeOffset.Parse("2026-08-11T08:00:00Z");
        Assert.Equal(received.AddHours(8), _service.CalculateDeadline(received, 8));
    }

    [Theory] [InlineData(0)] [InlineData(-1)]
    public void CalculateDeadline_RejectsNonPositiveHours(int hours) =>
        Assert.Throws<DomainRuleException>(() => _service.CalculateDeadline(DateTimeOffset.UtcNow, hours));

    [Theory]
    [InlineData(-1, "Vencido")] [InlineData(0, "Vencido")] [InlineData(2, "Próximo")] [InlineData(5, "En tiempo")]
    public void GetSlaState_ClassifiesDeadline(int deadlineOffsetHours, string expected)
    {
        var now = DateTimeOffset.Parse("2026-08-11T12:00:00Z");
        Assert.Equal(expected, _service.GetSlaState(now.AddHours(deadlineOffsetHours), now, 240));
    }

    [Fact]
    public void GetSlaState_RejectsInvalidAlertThreshold() =>
        Assert.Throws<DomainRuleException>(() => _service.GetSlaState(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0));

    private static SlaPolicy Policy(short? category, short priority, DateTimeOffset from, DateTimeOffset? until = null, bool active = true) =>
        new() { Id = Guid.NewGuid(), Name = "Test", CategoryId = category, PriorityId = priority, ResolutionHours = 8, AlertThresholdMinutes = 120, ValidFrom = from, ValidUntil = until, IsActive = active };
}
