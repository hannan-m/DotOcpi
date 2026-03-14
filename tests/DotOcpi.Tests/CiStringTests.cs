using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests;

public class CiStringTests
{
    [Fact]
    public void Equality_CaseInsensitive()
    {
        CiString a = "Hello";
        CiString b = "hello";
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_NotEqual()
    {
        CiString a = "Hello";
        CiString b = "World";
        a.Should().NotBe(b);
    }

    [Fact]
    public void GetHashCode_CaseInsensitive()
    {
        CiString a = "TEST";
        CiString b = "test";
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void ImplicitConversion_FromString()
    {
        CiString ci = "test";
        ci.Value.Should().Be("test");
    }

    [Fact]
    public void ImplicitConversion_ToString()
    {
        CiString ci = "test";
        string s = ci;
        s.Should().Be("test");
    }

    [Fact]
    public void ToString_PreservesOriginalCase()
    {
        CiString ci = "TestValue";
        ci.ToString().Should().Be("TestValue");
    }

    [Fact]
    public void Value_PreservesOriginalCase()
    {
        CiString ci = "MiXeD";
        ci.Value.Should().Be("MiXeD");
    }

    [Theory]
    [InlineData("abc", 3, true)]
    [InlineData("abc", 2, false)]
    [InlineData("abc", 10, true)]
    [InlineData("", 0, true)]
    public void IsWithinMaxLength_ReturnsCorrectly(string value, int maxLength, bool expected)
    {
        CiString ci = value;
        ci.IsWithinMaxLength(maxLength).Should().Be(expected);
    }

    [Fact]
    public void IsEmpty_EmptyString_ReturnsTrue()
    {
        CiString ci = "";
        ci.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_NonEmptyString_ReturnsFalse()
    {
        CiString ci = "value";
        ci.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Constructor_NullValue_Throws()
    {
        var act = () => new CiString(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EqualityOperator_CaseInsensitive()
    {
        CiString a = "ABC";
        CiString b = "abc";
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_DifferentValues()
    {
        CiString a = "ABC";
        CiString b = "XYZ";
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void CanBeUsedInDictionary()
    {
        var dict = new Dictionary<CiString, int> { ["Key"] = 1 };
        dict["key"].Should().Be(1);
        dict["KEY"].Should().Be(1);
    }

    [Fact]
    public void CanBeUsedInHashSet()
    {
        var set = new HashSet<CiString> { "Test" };
        set.Contains((CiString)"test").Should().BeTrue();
        set.Contains((CiString)"TEST").Should().BeTrue();
    }
}
