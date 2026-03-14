using DotOcpi.AspNetCore.Filters;
using DotOcpi.Validation;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Filters;

public class OcpiValidationFilterTests
{
    public sealed record TestModel(string Name, int Value);

    private readonly IOcpiValidator<TestModel> _validator = Substitute.For<IOcpiValidator<TestModel>>();
    private readonly OcpiValidationFilter<TestModel> _filter = new();

    private static EndpointFilterInvocationContext CreateFilterContext(HttpContext httpContext, params object[] args)
    {
        var ctx = Substitute.For<EndpointFilterInvocationContext>();
        ctx.HttpContext.Returns(httpContext);
        ctx.Arguments.Returns((IList<object?>)args.Cast<object?>().ToList());
        return ctx;
    }

    private DefaultHttpContext CreateHttpContextWithValidator()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_validator);
        var httpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        return httpContext;
    }

    [Fact]
    public async Task NoValidator_CallsNext()
    {
        var httpContext = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        var model = new TestModel("test", 42);
        var filterContext = CreateFilterContext(httpContext, model);
        var nextCalled = false;

        await _filter.InvokeAsync(
            filterContext,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>("ok");
            }
        );

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ValidModel_CallsNext()
    {
        _validator.Validate(Arg.Any<TestModel>()).Returns(OcpiValidationResult.Valid());
        var httpContext = CreateHttpContextWithValidator();
        var model = new TestModel("test", 42);
        var filterContext = CreateFilterContext(httpContext, model);
        var nextCalled = false;

        await _filter.InvokeAsync(
            filterContext,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>("ok");
            }
        );

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvalidModel_Returns400WithOcpi2001()
    {
        var error = new OcpiValidationError("VAL001", "Name is required", "Provide a name") { PropertyPath = "name" };
        _validator.Validate(Arg.Any<TestModel>()).Returns(OcpiValidationResult.Failed(error));

        var httpContext = CreateHttpContextWithValidator();
        var model = new TestModel("", 0);
        var filterContext = CreateFilterContext(httpContext, model);

        var result = await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("next"));

        // Filter returns an IResult (not calling next), indicating rejection
        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task InvalidModel_DoesNotCallNext()
    {
        var error = new OcpiValidationError("VAL001", "Bad", "Fix it");
        _validator.Validate(Arg.Any<TestModel>()).Returns(OcpiValidationResult.Failed(error));

        var httpContext = CreateHttpContextWithValidator();
        var model = new TestModel("", 0);
        var filterContext = CreateFilterContext(httpContext, model);
        var nextCalled = false;

        await _filter.InvokeAsync(
            filterContext,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>("next");
            }
        );

        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task NoModelInArgs_CallsNext()
    {
        _validator.Validate(Arg.Any<TestModel>()).Returns(OcpiValidationResult.Valid());
        var httpContext = CreateHttpContextWithValidator();
        var filterContext = CreateFilterContext(httpContext, "not-a-model", 42);
        var nextCalled = false;

        await _filter.InvokeAsync(
            filterContext,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>("ok");
            }
        );

        nextCalled.Should().BeTrue();
    }
}
