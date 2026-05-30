using FluentAssertions;
using KatiesGarden.Models.Shop;
using Xunit;

namespace KatiesGarden.Tests.Validators;

public class UpdateCollectionRequestValidatorTests
{
    private readonly UpdateCollectionRequestValidator _validator = new();

    private static UpdateCollectionRequest ValidRequest() => new()
    {
        Title = "Summer Collection",
        Description = "Seasonal summer picks.",
        IsActive = true,
        StartDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = null,
        DisplayOrder = 0
    };

    [Fact]
    public async Task AllValid_Passes()
    {
        var result = await _validator.ValidateAsync(ValidRequest(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyTitle_Fails(string value)
    {
        var req = ValidRequest(); req.Title = value;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCollectionRequest.Title));
    }

    [Fact]
    public async Task TitleOver200Chars_Fails()
    {
        var req = ValidRequest(); req.Title = new string('a', 201);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCollectionRequest.Title));
    }

    [Fact]
    public async Task TitleExactly200Chars_Passes()
    {
        var req = ValidRequest(); req.Title = new string('a', 200);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateCollectionRequest.Title));
    }

    [Fact]
    public async Task DescriptionOver2000Chars_Fails()
    {
        var req = ValidRequest(); req.Description = new string('a', 2001);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCollectionRequest.Description));
    }

    [Fact]
    public async Task DescriptionExactly2000Chars_Passes()
    {
        var req = ValidRequest(); req.Description = new string('a', 2000);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateCollectionRequest.Description));
    }

    [Fact]
    public async Task EndDateBeforeStartDate_Fails()
    {
        var req = ValidRequest();
        req.StartDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        req.EndDate = new DateTime(2025, 5, 31, 0, 0, 0, DateTimeKind.Utc);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCollectionRequest.EndDate));
    }

    [Fact]
    public async Task EndDateEqualToStartDate_Fails()
    {
        var req = ValidRequest();
        req.StartDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        req.EndDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateCollectionRequest.EndDate));
    }

    [Fact]
    public async Task EndDateAfterStartDate_Passes()
    {
        var req = ValidRequest();
        req.StartDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        req.EndDate = new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateCollectionRequest.EndDate));
    }

    [Fact]
    public async Task NullEndDate_Passes()
    {
        var req = ValidRequest(); req.EndDate = null;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateCollectionRequest.EndDate));
    }
}
