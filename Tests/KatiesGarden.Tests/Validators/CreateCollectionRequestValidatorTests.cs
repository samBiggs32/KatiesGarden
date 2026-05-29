using FluentAssertions;
using KatiesGarden.Models.Shop;
using Xunit;

namespace KatiesGarden.Tests.Validators;

public class CreateCollectionRequestValidatorTests
{
    private readonly CreateCollectionRequestValidator _validator = new();

    private static CreateCollectionRequest ValidRequest() => new()
    {
        Title = "Spring Sale 2025",
        Description = "Our spring collection of seasonal plants and wreaths.",
        StartDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2025, 5, 31, 0, 0, 0, DateTimeKind.Utc)
    };

    [Fact]
    public async Task AllValid_Passes()
    {
        var result = await _validator.ValidateAsync(ValidRequest(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task NoEndDate_Passes()
    {
        var req = ValidRequest(); req.EndDate = null;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyTitle_Fails(string value)
    {
        var req = ValidRequest(); req.Title = value;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCollectionRequest.Title));
    }

    [Fact]
    public async Task TitleOver200Chars_Fails()
    {
        var req = ValidRequest(); req.Title = new string('a', 201);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCollectionRequest.Title));
    }

    [Fact]
    public async Task TitleExactly200Chars_Passes()
    {
        var req = ValidRequest(); req.Title = new string('a', 200);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateCollectionRequest.Title));
    }

    [Fact]
    public async Task DescriptionOver2000Chars_Fails()
    {
        var req = ValidRequest(); req.Description = new string('a', 2001);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCollectionRequest.Description));
    }

    [Fact]
    public async Task EndDateBeforeStartDate_Fails()
    {
        var req = ValidRequest();
        req.StartDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        req.EndDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCollectionRequest.EndDate));
    }

    [Fact]
    public async Task EndDateSameAsStartDate_Fails()
    {
        var req = ValidRequest();
        req.StartDate = new DateTime(2025, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        req.EndDate = req.StartDate;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCollectionRequest.EndDate));
    }

    [Fact]
    public async Task EndDateAfterStartDate_Passes()
    {
        var req = ValidRequest();
        req.StartDate = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        req.EndDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CreateCollectionRequest.EndDate));
    }
}
