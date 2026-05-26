using FluentAssertions;
using KatiesGarden.Models.Helpers;
using Xunit;

namespace KatiesGarden.Tests.Helpers;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Spring Sale 2025", "spring-sale-2025")]
    [InlineData("Autumn Wreaths & Bouquets", "autumn-wreaths-bouquets")]
    [InlineData("  Leading and Trailing Spaces  ", "leading-and-trailing-spaces")]
    [InlineData("Multiple   Internal   Spaces", "multiple-internal-spaces")]
    [InlineData("Special £ ! @ # Chars", "special-chars")]
    [InlineData("already-a-slug", "already-a-slug")]
    [InlineData("Numbers 123 Preserved", "numbers-123-preserved")]
    [InlineData("UPPERCASE TITLE", "uppercase-title")]
    [InlineData("---leading-hyphens---", "leading-hyphens")]
    public void Generate_ReturnsExpectedSlug(string input, string expected)
    {
        SlugHelper.Generate(input).Should().Be(expected);
    }

    [Fact]
    public void Generate_EmptyString_ReturnsEmpty()
    {
        SlugHelper.Generate(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhitespaceOnly_ReturnsEmpty()
    {
        SlugHelper.Generate("   ").Should().BeEmpty();
    }

    [Fact]
    public void Generate_UnicodeLetters_AreStripped()
    {
        // Non-ASCII characters are not a-z0-9, so they get replaced
        var result = SlugHelper.Generate("Café au lait");
        result.Should().NotContain("é");
        result.Should().MatchRegex("^[a-z0-9-]*$");
    }
}
