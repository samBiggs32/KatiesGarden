using FluentAssertions;
using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Models.Validators;
using Xunit;

namespace KatiesGarden.Tests.Validators;

public class SubscribeRequestValidatorTests
{
    private readonly SubscribeRequestValidator _validator = new();

    [Fact]
    public void ValidEmail_NoFirstName_Passes()
    {
        var result = _validator.Validate(new SubscribeRequest("katie@example.com", null));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidEmail_WithFirstName_Passes()
    {
        var result = _validator.Validate(new SubscribeRequest("katie@example.com", "Katie"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EmptyEmail_Fails(string? email)
    {
        var result = _validator.Validate(new SubscribeRequest(email!, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@tld")]
    [InlineData("@nodomain.com")]
    [InlineData("spaces in@email.com")]
    public void InvalidEmailFormat_Fails(string email)
    {
        var result = _validator.Validate(new SubscribeRequest(email, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Email");
    }

    [Fact]
    public void EmailExceeding254Chars_Fails()
    {
        var longEmail = new string('a', 250) + "@b.com";
        var result = _validator.Validate(new SubscribeRequest(longEmail, null));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void FirstNameExceeding100Chars_Fails()
    {
        var result = _validator.Validate(new SubscribeRequest("valid@example.com", new string('a', 101)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void FirstNameExactly100Chars_Passes()
    {
        var result = _validator.Validate(new SubscribeRequest("valid@example.com", new string('a', 100)));
        result.IsValid.Should().BeTrue();
    }
}
