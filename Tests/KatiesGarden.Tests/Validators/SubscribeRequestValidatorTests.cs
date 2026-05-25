using FluentAssertions;
using KatiesGarden.Models;
using KatiesGarden.Models.Validators;
using Xunit;

namespace KatiesGarden.Tests.Validators;

public class SubscribeRequestValidatorTests
{
    private readonly SubscribeRequestValidator _validator = new();

    [Theory]
    [InlineData("katie@example.com", null)]
    [InlineData("katie@example.com", "Katie")]
    [InlineData("user+tag@example.com", "Katie")]
    [InlineData("first.last@sub.example.co.uk", "Katie-Anne")]
    [InlineData("k@x.io", null)]
    [InlineData("a@b.c", "Jose")]
    public void ValidInput_Passes(string email, string? firstName)
    {
        var result = _validator.Validate(new SubscribeRequest(email, firstName));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void EmptyEmail_FailsWithSingleRequiredError(string? email)
    {
        var result = _validator.Validate(new SubscribeRequest(email!, null));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Email")
            .Which.ErrorMessage.Should().Be("Email address is required.");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@tld")]
    [InlineData("@nodomain.com")]
    [InlineData("nolocal@.com")]
    [InlineData("spaces in@email.com")]
    [InlineData("trailing.space@example.com ")]
    [InlineData("two@@signs.com")]
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
    public void EmailExactly254Chars_Passes()
    {
        var local = new string('a', 254 - "@b.co".Length);
        var email = local + "@b.co";
        email.Length.Should().Be(254);
        var result = _validator.Validate(new SubscribeRequest(email, null));
        result.IsValid.Should().BeTrue();
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

    [Fact]
    public void NullFirstName_IsAccepted()
    {
        var result = _validator.Validate(new SubscribeRequest("valid@example.com", null));
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
