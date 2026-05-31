using FluentAssertions;
using KatiesGarden.Api.Helpers;
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
