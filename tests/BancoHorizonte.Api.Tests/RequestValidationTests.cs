using System.ComponentModel.DataAnnotations;
using BancoHorizonte.Api.Contracts;

namespace BancoHorizonte.Api.Tests;

public sealed class RequestValidationTests
{
    [Fact]
    public void ValidCreateComplaintRequest_HasNoValidationErrors() => Assert.Empty(ValidateRecursive(ValidRequest()));

    [Theory]
    [InlineData("", "0912345678", "Ana", "Vega", "a@b.com")]
    [InlineData("CEDULA", "123", "Ana", "Vega", "a@b.com")]
    [InlineData("CEDULA", "0912345678", "A", "Vega", "a@b.com")]
    [InlineData("CEDULA", "0912345678", "Ana", "V", "a@b.com")]
    [InlineData("CEDULA", "0912345678", "Ana", "Vega", "no-es-correo")]
    public void CustomerValidation_RejectsInvalidFields(string type, string number, string names, string lastNames, string email) =>
        Assert.NotEmpty(ValidateRecursive(new CustomerRequest(type, number, names, lastNames, email, "0990000000")));

    [Theory]
    [InlineData("CEDULA", "091234567", "0990000000")]
    [InlineData("CEDULA", "09123456789", "0990000000")]
    [InlineData("CEDULA", "09123A5678", "0990000000")]
    [InlineData("RUC", "179001234500", "0990000000")]
    [InlineData("PASAPORTE", "A-1234", "0990000000")]
    [InlineData("CEDULA", "0912345678", "099000000")]
    [InlineData("CEDULA", "0912345678", "09900000000")]
    [InlineData("CEDULA", "0912345678", "09900A0000")]
    public void CustomerValidation_RequiresExactDocumentAndPhoneFormats(string type, string number, string phone) =>
        Assert.NotEmpty(ValidateRecursive(new CustomerRequest(type, number, "Ana", "Vega", "ana@example.com", phone)));

    [Theory]
    [InlineData(0, 1, 10, "Descripción válida con más de veinte caracteres")]
    [InlineData(1, 0, 10, "Descripción válida con más de veinte caracteres")]
    [InlineData(1, 1, -1, "Descripción válida con más de veinte caracteres")]
    [InlineData(1, 1, 10, "muy corta")]
    public void CreateComplaintValidation_RejectsInvalidFields(short channel, short category, double amount, string description)
    {
        var request = ValidRequest() with { ReceptionChannelId = channel, CategoryId = category,
            AffectedAmount = (decimal)amount, Description = description };
        Assert.NotEmpty(ValidateRecursive(request));
    }

    [Theory] [InlineData("")] [InlineData("ab")]
    public void ObservationValidation_RequiresAtLeastThreeCharacters(string content) =>
        Assert.NotEmpty(ValidateRecursive(new AddObservationRequest(content)));

    [Theory] [InlineData("")] [InlineData("ab")]
    public void StatusChangeValidation_RequiresObservation(string observation) =>
        Assert.NotEmpty(ValidateRecursive(new ChangeStatusRequest(2, observation)));

    [Fact]
    public void AssignmentValidation_RejectsEmptyAnalystAndLongReason() =>
        Assert.NotEmpty(ValidateRecursive(new AssignComplaintRequest(Guid.Empty, new string('x', 501))));

    private static CreateComplaintRequest ValidRequest() => new(
        new CustomerRequest("CEDULA", "0912345678", "Ana", "Vega", "ana@example.com", "0990000000"),
        1, 1, null, "Descripción válida con más de veinte caracteres.", 25, false, DateTimeOffset.UtcNow);

    private static List<ValidationResult> ValidateRecursive(object instance)
    {
        var results = new List<ValidationResult>();
        ValidateObject(instance, results);
        foreach (var property in instance.GetType().GetProperties())
        {
            var value = property.GetValue(instance);
            if (value is not null && value.GetType().Namespace == typeof(CustomerRequest).Namespace)
                ValidateObject(value, results);
        }
        return results;
    }

    private static void ValidateObject(object instance, List<ValidationResult> results)
    {
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);
        var type = instance.GetType();
        var constructor = type.GetConstructors().OrderByDescending(x => x.GetParameters().Length).FirstOrDefault();
        if (constructor is null) return;
        foreach (var parameter in constructor.GetParameters())
        {
            var property = type.GetProperties().FirstOrDefault(x => string.Equals(x.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
            var attributes = parameter.GetCustomAttributes(typeof(ValidationAttribute), true).Cast<ValidationAttribute>();
            Validator.TryValidateValue(property?.GetValue(instance), new ValidationContext(instance)
                { MemberName = property?.Name ?? parameter.Name }, results, attributes);
        }
    }
}
