using System.Text.Json;
using DotOcpi.Serialization;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Serialization;

public class PatchHelperTests
{
    [Fact]
    public void ApplyPatch_UpdatesExistingField()
    {
        var target = Parse("""{"name":"Alice","age":30}""");
        var patch = Parse("""{"age":31}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        result.GetProperty("name").GetString().Should().Be("Alice");
        result.GetProperty("age").GetInt32().Should().Be(31);
    }

    [Fact]
    public void ApplyPatch_AddsNewField()
    {
        var target = Parse("""{"name":"Alice"}""");
        var patch = Parse("""{"email":"alice@example.com"}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        result.GetProperty("name").GetString().Should().Be("Alice");
        result.GetProperty("email").GetString().Should().Be("alice@example.com");
    }

    [Fact]
    public void ApplyPatch_RemovesFieldWithExplicitNull()
    {
        var target = Parse("""{"name":"Alice","age":30}""");
        var patch = Parse("""{"age":null}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        result.GetProperty("name").GetString().Should().Be("Alice");
        result.TryGetProperty("age", out _).Should().BeFalse();
    }

    [Fact]
    public void ApplyPatch_LeavesUnmentionedFieldsUnchanged()
    {
        var target = Parse("""{"a":1,"b":2,"c":3}""");
        var patch = Parse("""{"b":20}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        result.GetProperty("a").GetInt32().Should().Be(1);
        result.GetProperty("b").GetInt32().Should().Be(20);
        result.GetProperty("c").GetInt32().Should().Be(3);
    }

    [Fact]
    public void ApplyPatch_MergesNestedObjects()
    {
        var target = Parse("""{"address":{"street":"Main St","city":"Amsterdam"}}""");
        var patch = Parse("""{"address":{"city":"Rotterdam"}}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        var address = result.GetProperty("address");
        address.GetProperty("street").GetString().Should().Be("Main St");
        address.GetProperty("city").GetString().Should().Be("Rotterdam");
    }

    [Fact]
    public void ApplyPatch_ReplacesNestedObjectFieldWithNull()
    {
        var target = Parse("""{"address":{"street":"Main St","city":"Amsterdam"}}""");
        var patch = Parse("""{"address":{"city":null}}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        var address = result.GetProperty("address");
        address.GetProperty("street").GetString().Should().Be("Main St");
        address.TryGetProperty("city", out _).Should().BeFalse();
    }

    [Fact]
    public void ApplyPatch_ReplacesEntireNestedObjectWithNull()
    {
        var target = Parse("""{"name":"Alice","address":{"street":"Main St"}}""");
        var patch = Parse("""{"address":null}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        result.GetProperty("name").GetString().Should().Be("Alice");
        result.TryGetProperty("address", out _).Should().BeFalse();
    }

    [Fact]
    public void ApplyPatch_ReplacesObjectWithScalar()
    {
        var target = Parse("""{"data":{"nested":true}}""");
        var patch = Parse("""{"data":"flat"}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        result.GetProperty("data").GetString().Should().Be("flat");
    }

    [Fact]
    public void ApplyPatch_ReplacesScalarWithObject()
    {
        var target = Parse("""{"data":"flat"}""");
        var patch = Parse("""{"data":{"nested":true}}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        result.GetProperty("data").GetProperty("nested").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void ApplyPatch_ReplacesArrayEntirely()
    {
        var target = Parse("""{"tags":["a","b"]}""");
        var patch = Parse("""{"tags":["c"]}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        var tags = result.GetProperty("tags");
        tags.GetArrayLength().Should().Be(1);
        tags[0].GetString().Should().Be("c");
    }

    [Fact]
    public void ApplyPatch_NonObjectPatchReplacesTarget()
    {
        var target = Parse("""{"name":"Alice"}""");
        var patch = Parse("""42""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        result.GetInt32().Should().Be(42);
    }

    [Fact]
    public void ApplyPatch_PatchOntoNonObject()
    {
        var target = Parse("""42""");
        var patch = Parse("""{"name":"Alice"}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        result.GetProperty("name").GetString().Should().Be("Alice");
    }

    [Fact]
    public void ApplyPatch_EmptyPatch_LeavesTargetUnchanged()
    {
        var target = Parse("""{"name":"Alice","age":30}""");
        var patch = Parse("""{}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        result.GetProperty("name").GetString().Should().Be("Alice");
        result.GetProperty("age").GetInt32().Should().Be(30);
    }

    [Fact]
    public void ApplyPatch_EmptyTarget_CreatesFromPatch()
    {
        var target = Parse("""{}""");
        var patch = Parse("""{"name":"Alice"}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        result.GetProperty("name").GetString().Should().Be("Alice");
    }

    [Fact]
    public void ApplyPatch_DeeplyNestedMerge()
    {
        var target = Parse("""{"a":{"b":{"c":{"d":1,"e":2}}}}""");
        var patch = Parse("""{"a":{"b":{"c":{"e":3,"f":4}}}}""");

        var result = OcpiPatchHelper.ApplyPatch(target, patch);

        var c = result.GetProperty("a").GetProperty("b").GetProperty("c");
        c.GetProperty("d").GetInt32().Should().Be(1);
        c.GetProperty("e").GetInt32().Should().Be(3);
        c.GetProperty("f").GetInt32().Should().Be(4);
    }

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;
}
