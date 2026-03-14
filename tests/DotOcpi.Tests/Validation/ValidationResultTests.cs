using DotOcpi.Validation;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Validation;

public class ValidationResultTests
{
    [Fact]
    public void Valid_ReturnsNoErrors()
    {
        var result = OcpiValidationResult.Valid();

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Valid_ReturnsSameInstance()
    {
        var a = OcpiValidationResult.Valid();
        var b = OcpiValidationResult.Valid();

        a.Should().BeSameAs(b);
    }

    [Fact]
    public void Failed_SingleError_IsInvalid()
    {
        var error = new OcpiValidationError("TEST_CODE", "Something failed", "Fix it");

        var result = OcpiValidationResult.Failed(error);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("TEST_CODE");
    }

    [Fact]
    public void Failed_MultipleErrors_ContainsAll()
    {
        var errors = new[]
        {
            new OcpiValidationError("ERR_1", "First", "Fix first"),
            new OcpiValidationError("ERR_2", "Second", "Fix second"),
        };

        var result = OcpiValidationResult.Failed(errors);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Error_IncludesPropertyPath()
    {
        var error = new OcpiValidationError("TEST", "Bad value", "Fix it") { PropertyPath = "Evses[0].Status" };

        error.PropertyPath.Should().Be("Evses[0].Status");
    }

    [Fact]
    public void Error_PropertyPathDefaultsToNull()
    {
        var error = new OcpiValidationError("TEST", "Bad value", "Fix it");

        error.PropertyPath.Should().BeNull();
    }
}
