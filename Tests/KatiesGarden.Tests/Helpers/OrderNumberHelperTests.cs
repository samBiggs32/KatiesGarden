using FluentAssertions;
using KatiesGarden.Api.Helpers;
using System.Text.RegularExpressions;
using Xunit;

namespace KatiesGarden.Tests.Helpers;

public class OrderNumberHelperTests
{
    [Fact]
    public void Generate_MatchesExpectedFormat()
    {
        var orderNumber = OrderNumberHelper.Generate();
        orderNumber.Should().MatchRegex(@"^KG-\d{8}-[0-9A-F]{4}$");
    }

    [Fact]
    public void Generate_StartsWithKgPrefix()
    {
        OrderNumberHelper.Generate().Should().StartWith("KG-");
    }

    [Fact]
    public void Generate_TodayDateMatches()
    {
        var orderNumber = OrderNumberHelper.Generate();
        var expectedDate = DateTime.UtcNow.ToString("yyyyMMdd");
        orderNumber.Should().Contain(expectedDate);
    }

    [Fact]
    public void Generate_HasFourCharHexSuffix()
    {
        var orderNumber = OrderNumberHelper.Generate();
        var match = Regex.Match(orderNumber, @"-(?<suffix>[0-9A-F]{4})$");
        match.Success.Should().BeTrue();
        match.Groups["suffix"].Value.Should().HaveLength(4);
    }

    [Fact]
    public void Generate_ProducesUniqueValues_AcrossManyCalls()
    {
        // Birthday-paradox math: with 4-hex-char (65536 possibilities), 100 generations
        // have ~7% collision chance. To be deterministic, just assert > 95% are unique.
        var seen = new HashSet<string>();
        const int iterations = 100;
        for (var i = 0; i < iterations; i++)
            seen.Add(OrderNumberHelper.Generate());

        seen.Count.Should().BeGreaterThan(95, "collisions should be rare with a 4-char hex suffix");
    }
}
