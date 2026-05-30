using FluentAssertions;
using KatiesGarden.Models.Shop;
using Xunit;

namespace KatiesGarden.Tests.Validators;

public class UpdateProductRequestValidatorTests
{
    private readonly UpdateProductRequestValidator _validator = new();

    private static UpdateProductRequest ValidRequest() => new()
    {
        Name = "Summer Wreath",
        Description = "A beautiful seasonal wreath.",
        Price = 25.00m,
        IsAvailable = true,
        CanLocalDeliver = true,
        ImageUrls = []
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
    public async Task EmptyName_Fails(string value)
    {
        var req = ValidRequest(); req.Name = value;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductRequest.Name));
    }

    [Fact]
    public async Task NameOver200Chars_Fails()
    {
        var req = ValidRequest(); req.Name = new string('a', 201);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductRequest.Name));
    }

    [Fact]
    public async Task NameExactly200Chars_Passes()
    {
        var req = ValidRequest(); req.Name = new string('a', 200);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateProductRequest.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public async Task PriceNotPositive_Fails(double price)
    {
        var req = ValidRequest(); req.Price = (decimal)price;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductRequest.Price));
    }

    [Fact]
    public async Task PriceMinimumPositive_Passes()
    {
        var req = ValidRequest(); req.Price = 0.01m;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateProductRequest.Price));
    }

    [Fact]
    public async Task DescriptionOver2000Chars_Fails()
    {
        var req = ValidRequest(); req.Description = new string('a', 2001);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductRequest.Description));
    }

    [Fact]
    public async Task DescriptionExactly2000Chars_Passes()
    {
        var req = ValidRequest(); req.Description = new string('a', 2000);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateProductRequest.Description));
    }

    [Fact]
    public async Task NullStockQuantity_Passes()
    {
        var req = ValidRequest(); req.StockQuantity = null;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateProductRequest.StockQuantity));
    }

    [Fact]
    public async Task ZeroStockQuantity_Passes()
    {
        var req = ValidRequest(); req.StockQuantity = 0;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateProductRequest.StockQuantity));
    }

    [Fact]
    public async Task NegativeStockQuantity_Fails()
    {
        var req = ValidRequest(); req.StockQuantity = -1;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductRequest.StockQuantity));
    }

    [Fact]
    public async Task HowToBuyNoteOver500Chars_Fails()
    {
        var req = ValidRequest(); req.HowToBuyNote = new string('a', 501);
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductRequest.HowToBuyNote));
    }

    [Fact]
    public async Task HowToBuyNoteNull_Passes()
    {
        var req = ValidRequest(); req.HowToBuyNote = null;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateProductRequest.HowToBuyNote));
    }

    [Fact]
    public async Task SevenImages_Fails()
    {
        var req = ValidRequest(); req.ImageUrls = Enumerable.Repeat("https://example.com/img.jpg", 7).ToArray();
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProductRequest.ImageUrls));
    }

    [Fact]
    public async Task SixImages_Passes()
    {
        var req = ValidRequest(); req.ImageUrls = Enumerable.Repeat("https://example.com/img.jpg", 6).ToArray();
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(UpdateProductRequest.ImageUrls));
    }
}
