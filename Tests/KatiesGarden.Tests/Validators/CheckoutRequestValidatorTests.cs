using FluentAssertions;
using KatiesGarden.Models.Entities;
using KatiesGarden.Models.Shop;
using Xunit;

namespace KatiesGarden.Tests.Validators;

public class CheckoutRequestValidatorTests
{
    private readonly CheckoutRequestValidator _validator = new();

    private static CheckoutRequest ValidCollectionRequest() => new()
    {
        FirstName = "Jane",
        LastName = "Smith",
        Email = "jane.smith@example.com",
        Phone = "07800 123456",
        DeliveryType = DeliveryType.Collection,
        Items = [new CartItemRequest { ProductId = Guid.NewGuid(), Quantity = 1 }]
    };

    private static CheckoutRequest ValidDeliveryRequest() => new()
    {
        FirstName = "Jane",
        LastName = "Smith",
        Email = "jane.smith@example.com",
        Phone = "07800 123456",
        DeliveryType = DeliveryType.LocalDelivery,
        DeliveryAddress = "12 High Street, Milverton",
        DeliveryPostcode = "TA4 1JN",
        Items = [new CartItemRequest { ProductId = Guid.NewGuid(), Quantity = 2 }]
    };

    [Fact]
    public async Task ValidCollection_Passes()
    {
        var result = await _validator.ValidateAsync(ValidCollectionRequest(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidLocalDelivery_Passes()
    {
        var result = await _validator.ValidateAsync(ValidDeliveryRequest(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyFirstName_Fails(string value)
    {
        var req = ValidCollectionRequest(); req.FirstName = value;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutRequest.FirstName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyLastName_Fails(string value)
    {
        var req = ValidCollectionRequest(); req.LastName = value;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutRequest.LastName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("two@@signs.com")]
    [InlineData("user@tld")]              // no dot in domain — passed the old .EmailAddress() check
    [InlineData("first name@example.com")] // whitespace in local part
    public async Task InvalidEmail_Fails(string email)
    {
        var req = ValidCollectionRequest(); req.Email = email;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutRequest.Email));
    }

    [Theory]
    [InlineData("jane@example.com")]
    [InlineData("user+tag@domain.co.uk")]
    public async Task ValidEmail_Passes(string email)
    {
        var req = ValidCollectionRequest(); req.Email = email;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CheckoutRequest.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyPhone_Fails(string value)
    {
        var req = ValidCollectionRequest(); req.Phone = value;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutRequest.Phone));
    }

    [Fact]
    public async Task LocalDelivery_WithoutAddress_Fails()
    {
        var req = ValidDeliveryRequest();
        req.DeliveryAddress = null;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutRequest.DeliveryAddress));
    }

    [Fact]
    public async Task LocalDelivery_WithoutPostcode_Fails()
    {
        var req = ValidDeliveryRequest();
        req.DeliveryPostcode = null;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutRequest.DeliveryPostcode));
    }

    [Fact]
    public async Task Collection_WithoutAddress_Passes()
    {
        var req = ValidCollectionRequest();
        req.DeliveryAddress = null;
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(CheckoutRequest.DeliveryAddress));
    }

    [Fact]
    public async Task EmptyItems_Fails()
    {
        var req = ValidCollectionRequest(); req.Items = [];
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutRequest.Items));
    }

    [Fact]
    public async Task ItemWithZeroQuantity_Fails()
    {
        var req = ValidCollectionRequest();
        req.Items = [new CartItemRequest { ProductId = Guid.NewGuid(), Quantity = 0 }];
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidDeliveryType_Fails()
    {
        var req = ValidCollectionRequest();
        req.DeliveryType = (DeliveryType)99; // out-of-range enum value
        var result = await _validator.ValidateAsync(req, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CheckoutRequest.DeliveryType));
    }
}
