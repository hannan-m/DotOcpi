using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests;

public class OcpiResultTests
{
    [Fact]
    public void GenericSuccess_SetsProperties()
    {
        var result = OcpiResult<string>.Success("data", "ok");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("data");
        result.StatusCode.Should().Be(OcpiStatusCode.Success);
        result.StatusMessage.Should().Be("ok");
    }

    [Fact]
    public void GenericSuccess_WithoutMessage_MessageIsNull()
    {
        var result = OcpiResult<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(42);
        result.StatusMessage.Should().BeNull();
    }

    [Fact]
    public void GenericFailure_SetsProperties()
    {
        var result = OcpiResult<string>.Failure(OcpiStatusCode.InvalidParameters, "bad input");

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.StatusCode.Should().Be(OcpiStatusCode.InvalidParameters);
        result.StatusMessage.Should().Be("bad input");
    }

    [Fact]
    public void NonGenericSuccess_SetsProperties()
    {
        var result = OcpiResult.Success("done");

        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(OcpiStatusCode.Success);
        result.StatusMessage.Should().Be("done");
    }

    [Fact]
    public void NonGenericSuccess_WithoutMessage_MessageIsNull()
    {
        var result = OcpiResult.Success();

        result.IsSuccess.Should().BeTrue();
        result.StatusMessage.Should().BeNull();
    }

    [Fact]
    public void NonGenericFailure_SetsProperties()
    {
        var result = OcpiResult.Failure(OcpiStatusCode.GenericServerError, "internal error");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(OcpiStatusCode.GenericServerError);
        result.StatusMessage.Should().Be("internal error");
    }

    [Fact]
    public void GenericSuccess_StatusCode_IsSuccess()
    {
        var result = OcpiResult<string>.Success("data");
        result.StatusCode.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void GenericFailure_StatusCode_IsClientError()
    {
        var result = OcpiResult<string>.Failure(OcpiStatusCode.NotEnoughInformation, "missing token");
        result.StatusCode.IsClientError.Should().BeTrue();
    }

    [Fact]
    public void GenericFailure_StatusCode_IsServerError()
    {
        var result = OcpiResult<string>.Failure(OcpiStatusCode.GenericServerError, "boom");
        result.StatusCode.IsServerError.Should().BeTrue();
    }
}
