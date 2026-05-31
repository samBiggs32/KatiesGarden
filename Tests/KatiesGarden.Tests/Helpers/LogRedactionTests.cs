using FluentAssertions;
using KatiesGarden.Api.Helpers;
using Xunit;

namespace KatiesGarden.Tests.Helpers;

public class LogRedactionTests
{
    [Fact]
    public void Hash_NullInput_ReturnsNone() =>
        LogRedaction.Hash(null).Should().Be("(none)");

    [Fact]
    public void Hash_EmptyString_ReturnsNone() =>
        LogRedaction.Hash("").Should().Be("(none)");

    [Fact]
    public void Hash_ValidEmail_Returns12HexChars()
    {
        var result = LogRedaction.Hash("user@example.com");
        result.Should().MatchRegex("^[0-9a-f]{12}$");
    }

    [Fact]
    public void Hash_SameInput_ProducesSameOutput()
    {
        var a = LogRedaction.Hash("user@example.com");
        var b = LogRedaction.Hash("user@example.com");
        a.Should().Be(b);
    }

    [Fact]
    public void Hash_CaseNormalized_SameHashForUpperAndLower()
    {
        var lower = LogRedaction.Hash("user@example.com");
        var upper = LogRedaction.Hash("USER@EXAMPLE.COM");
        lower.Should().Be(upper);
    }

    [Fact]
    public void Hash_LeadingTrailingWhitespace_NormalizedBeforeHashing()
    {
        var trimmed = LogRedaction.Hash("user@example.com");
        var padded  = LogRedaction.Hash("  user@example.com  ");
        trimmed.Should().Be(padded);
    }

    [Fact]
    public void Hash_DifferentInputs_ProduceDifferentOutputs()
    {
        var a = LogRedaction.Hash("user@example.com");
        var b = LogRedaction.Hash("other@example.com");
        a.Should().NotBe(b);
    }
}
