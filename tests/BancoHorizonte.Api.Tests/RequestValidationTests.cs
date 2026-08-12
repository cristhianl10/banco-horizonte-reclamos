using System.ComponentModel.DataAnnotations;
using BancoHorizonte.Api.Contracts;

namespace BancoHorizonte.Api.Tests;

public sealed class RequestValidationTests
{
    [Fact]
    public void ValidCreateComplaintRequest_HasNoValidationErrors() => Assert.Empty(ValidateRecursive(ValidRequest()));

    [Theory]
    [InlineData("", "12345", "Ana", "Vega", "a@b.com")]
    [InlineData("CED", "123", "Ana", "Vega", "a@b.com")]
    [InlineData("CED", "12345", "A", "Vega", "a@b.com")]
    [InlineData("CED", "12345", "Ana", "V", "a@b.com")]
    [InlineData("CED", "12345", "Ana", "Vega", "no-es-correo")]
    public void CustomerValidation_RejectsInvalidFields(string type, string number, string names, string lastNames, string email)
    {
        Assert.NotEmpty(ValidateRecursive(new CustomerRequest(type, number, names, lastNames, email, "0990000000")));
    }

    [Theory]
    [InlineData(0, 1, 1, "Descripción válida con más de veinte caracteres")]
    [InlineData(1, 0, 1, "Descripción válida con más de veinte caracteres")]
    [InlineData(1, 1, 0, "Descripción válida con más de veinte caracteres")]
    [InlineData(1, 1, 4, "Descripción válida con más de veinte caracteres")]
    [InlineData(1, 1, 1, "muy corta")]
    public void CreateComplaintValidation_RejectsInvalidClassification(short channel, short category, short impact, string description)
    {
        var request = ValidRequest() with { ReceptionChannelId = channel, CategoryId = category, Impact = impact, Description = description };
        Assert.NotEmpty(ValidateRecursive(request));
    }

    [Theory] [InlineData("")] [InlineData("ab")]
    public void ObservationValidation_RequiresAtLeastThreeCharacters(string content) =>
        Assert.NotEmpty(ValidateRecursive(new AddObservationRequest(content)));

    [Fact]
    public void AssignmentValidation_RejectsEmptyAnalystAndLongReason() =>
        Assert.NotEmpty(ValidateRecursive(new AssignComplaintRequest(Guid.Empty, new string('x', 501))));

    private static CreateComplaintRequest ValidRequest() => new(
        new CustomerRequest("CEDULA", "0912345678", "Ana", "Vega", "ana@example.com", "0990000000"),
        1, 1, null, "Descripción válida con más de veinte caracteres.", 2, 2);

    private static List<ValidationResult> ValidateRecursive(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);
        foreach (var property in instance.GetType().GetProperties())
        {
            var value = property.GetValue(instance);
            if (value is not null && value.GetType().Namespace == typeof(CustomerRequest).Namespace)
                Validator.TryValidateObject(value, new ValidationContext(value), results, true);
        }
        return results;
    }
}
