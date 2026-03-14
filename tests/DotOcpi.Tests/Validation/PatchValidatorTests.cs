using System.Text.Json;
using DotOcpi.Validation;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Validation;

public class PatchValidatorTests
{
    private static readonly PatchValidator LocationPatchValidator = new([
        "country_code",
        "party_id",
        "id",
        "publish",
        "name",
        "address",
        "city",
        "postal_code",
        "country",
        "coordinates",
        "time_zone",
        "last_updated",
        "evses",
        "parking_type",
        "state",
    ]);

    [Fact]
    public void Validate_ValidPatch_ReturnsValid()
    {
        var patch = JsonDocument.Parse("""{"name":"New Name","city":"Rotterdam"}""").RootElement;

        var result = LocationPatchValidator.Validate(patch);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NonObjectPatch_ReturnsError()
    {
        var patch = JsonDocument.Parse("""42""").RootElement;

        var result = LocationPatchValidator.Validate(patch);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "PATCH_NOT_OBJECT");
    }

    [Fact]
    public void Validate_ArrayPatch_ReturnsError()
    {
        var patch = JsonDocument.Parse("""[1,2,3]""").RootElement;

        var result = LocationPatchValidator.Validate(patch);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "PATCH_NOT_OBJECT");
    }

    [Fact]
    public void Validate_UnknownField_ReturnsError()
    {
        var patch = JsonDocument.Parse("""{"name":"Test","unknown_field":"value"}""").RootElement;

        var result = LocationPatchValidator.Validate(patch);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "PATCH_UNKNOWN_FIELD");
        result.Errors.Should().ContainSingle().Which.PropertyPath.Should().Be("unknown_field");
    }

    [Fact]
    public void Validate_MultipleUnknownFields_ReportsAll()
    {
        var patch = JsonDocument.Parse("""{"bad_field_1":"a","bad_field_2":"b"}""").RootElement;

        var result = LocationPatchValidator.Validate(patch);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Validate_NullValueField_IsAllowed()
    {
        var patch = JsonDocument.Parse("""{"name":null}""").RootElement;

        var result = LocationPatchValidator.Validate(patch);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyPatch_ReturnsValid()
    {
        var patch = JsonDocument.Parse("""{}""").RootElement;

        var result = LocationPatchValidator.Validate(patch);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_FieldNameIsCaseSensitive()
    {
        var patch = JsonDocument.Parse("""{"Name":"Test"}""").RootElement;

        var result = LocationPatchValidator.Validate(patch);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "PATCH_UNKNOWN_FIELD");
    }
}
