using BancoHorizonte.Api.Application;
using BancoHorizonte.Api.Contracts;
using BancoHorizonte.Api.Domain;

namespace BancoHorizonte.Api.Tests;

public sealed class PriorityAndSlaServiceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-11T12:00:00Z");
    private readonly PriorityAndSlaService _service = new();

    [Theory]
    [InlineData("Atención al cliente", "Demora en atención", null, false, 0, "Baja", 24)]
    [InlineData("Tarjetas", "Compra no reconocida", null, false, 4, "Media", 12)]
    [InlineData("Transferencias", "Transferencia no acreditada", 100, false, 3, "Media", 12)]
    [InlineData("Canales digitales", "Acceso o canal bloqueado", null, true, 5, "Alta", 6)]
    [InlineData("Tarjetas", "Compra no reconocida", 780, false, 7, "Crítica", 2)]
    public void Calculate_AppliesDocumentRules(string category, string subcategory, double? amount, bool unavailable,
        short score, string level, int hours)
    {
        var result = _service.Calculate(Input(category, subcategory, amount is null ? null : (decimal)amount, unavailable));
        Assert.Equal(score, result.Score);
        Assert.Equal(level, result.Level);
        Assert.Equal(hours, result.SlaHours);
    }

    [Fact]
    public void Calculate_AddsAgeRuleOnlyWhileOpen()
    {
        var open = _service.Calculate(Input("Atención al cliente", null, receivedAt: Now.AddHours(-25)));
        var closed = _service.Calculate(Input("Atención al cliente", null, receivedAt: Now.AddHours(-25), isOpen: false));
        Assert.Equal(2, open.Score);
        Assert.Equal(0, closed.Score);
    }

    [Fact]
    public void Calculate_IsAdditiveAcrossAllMatchingConditions()
    {
        var result = _service.Calculate(Input("Tarjetas", "Compra no reconocida", 900, true, Now.AddHours(-25)));
        Assert.Equal(11, result.Score);
        Assert.Equal(4, result.MatchedRules.Count);
        Assert.Equal("Crítica", result.Level);
    }

    [Fact]
    public void Calculate_RejectsFutureReception() =>
        Assert.Throws<DomainRuleException>(() => _service.Calculate(Input("Cobros", null, receivedAt: Now.AddHours(1))));

    [Fact]
    public void Calculate_RejectsNegativeAmount() =>
        Assert.Throws<DomainRuleException>(() => _service.Calculate(Input("Cobros", null, -1)));

    [Fact]
    public void SelectPolicy_PrefersCategorySpecificPolicy()
    {
        var generic = Policy(null, 4, Now.AddDays(-1));
        var specific = Policy(2, 4, Now.AddDays(-1));
        Assert.Same(specific, _service.SelectPolicy([generic, specific], 4, 2, Now));
    }

    [Fact]
    public void SelectPolicy_FallsBackToGenericPolicy()
    {
        var generic = Policy(null, 3, Now.AddDays(-1));
        Assert.Same(generic, _service.SelectPolicy([generic], 3, 9, Now));
    }

    [Fact]
    public void SelectPolicy_RejectsUnavailablePolicies()
    {
        var policies = new[] { Policy(null, 1, Now.AddDays(-2), Now.AddDays(-1)), Policy(null, 1, Now.AddDays(1)) };
        Assert.Throws<DomainRuleException>(() => _service.SelectPolicy(policies, 1, 1, Now));
    }

    [Fact]
    public void CalculateDeadline_AddsResolutionHours() =>
        Assert.Equal(Now.AddHours(6), _service.CalculateDeadline(Now, 6));

    [Fact]
    public void CalculateAlertAt_UsesSeventyFivePercent()
    {
        var deadline = Now.AddHours(12);
        Assert.Equal(Now.AddHours(9), _service.CalculateAlertAt(Now, deadline));
    }

    [Theory]
    [InlineData(13, "Vencido")]
    [InlineData(10, "Próximo")]
    [InlineData(8, "En tiempo")]
    public void GetSlaState_UsesAlertAt(int elapsedHours, string expected)
    {
        var deadline = Now.AddHours(12);
        var alert = Now.AddHours(9);
        Assert.Equal(expected, _service.GetSlaState(deadline, alert, Now.AddHours(elapsedHours)));
    }

    private static PriorityInput Input(string category, string? subcategory, decimal? amount = null, bool unavailable = false,
        DateTimeOffset? receivedAt = null, bool isOpen = true) =>
        new(category, subcategory, amount, unavailable, receivedAt ?? Now, Now, isOpen);

    private static SlaPolicy Policy(short? category, short priority, DateTimeOffset from, DateTimeOffset? until = null) =>
        new() { Id = Guid.NewGuid(), Name = "Test", CategoryId = category, PriorityId = priority, ResolutionHours = 2,
            AlertThresholdMinutes = 90, ValidFrom = from, ValidUntil = until, IsActive = true };
}
