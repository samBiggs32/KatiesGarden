using FluentAssertions;
using KatiesGarden.Api.Helpers;
using Xunit;

namespace KatiesGarden.Tests.Helpers;

public class OrderVerificationTests
{
    [Theory]
    [InlineData("2499")]        // digits only
    [InlineData("24.99")]       // decimal
    [InlineData("£24.99")]      // currency symbol
    [InlineData("  24.99  ")]   // surrounding whitespace ignored via digit extraction
    public void TotalMatches_EquivalentRepresentations_ReturnTrue(string input) =>
        OrderVerification.TotalMatches(24.99m, input).Should().BeTrue();

    [Theory]
    [InlineData("25.00")]  // wrong amount
    [InlineData("2498")]   // off by one pence
    [InlineData("2500")]   // off by one pence the other way
    public void TotalMatches_WrongAmount_ReturnsFalse(string input) =>
        OrderVerification.TotalMatches(24.99m, input).Should().BeFalse();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TotalMatches_NullOrWhitespace_ReturnsFalse(string? input) =>
        OrderVerification.TotalMatches(24.99m, input).Should().BeFalse();

    [Fact]
    public void TotalMatches_NoDigitsInInput_ReturnsFalse() =>
        OrderVerification.TotalMatches(24.99m, "£££").Should().BeFalse();

    [Fact]
    public void TotalMatches_ZeroTotal_MatchesDecimalRepresentation() =>
        OrderVerification.TotalMatches(0.00m, "0.00").Should().BeTrue();

    [Fact]
    public void TotalMatches_WholeNumberTotal_MatchesWithoutDecimal() =>
        OrderVerification.TotalMatches(10.00m, "1000").Should().BeTrue();
}
