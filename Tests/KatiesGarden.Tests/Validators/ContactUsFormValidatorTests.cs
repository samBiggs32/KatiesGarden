using FluentAssertions;
using KatiesGarden.Models;
using KatiesGarden.Models.Validators;
using Xunit;

namespace KatiesGarden.Tests.Validators;

public class ContactUsFormValidatorTests
{
    private readonly ContactUsFormValidator _validator = new();

    private static ContactUsForm ValidForm() => new()
    {
        FirstName = "John",
        LastName = "Smith",
        EmailAddress = "john.smith@example.com",
        ContactNumber = "07800 123456",
        EmailSubject = "Garden enquiry",
        EmailBody = "I would love to know more about your services."
    };

    [Fact]
    public async Task AllValid_Passes()
    {
        var result = await _validator.ValidateAsync(ValidForm(), TestContext.Current.CancellationToken);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyFirstName_Fails(string value)
    {
        var form = ValidForm(); form.FirstName = value;
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ContactUsForm.FirstName));
    }

    [Fact]
    public async Task FirstNameOver100Chars_Fails()
    {
        var form = ValidForm(); form.FirstName = new string('a', 101);
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ContactUsForm.FirstName));
    }

    [Fact]
    public async Task FirstNameExactly100Chars_Passes()
    {
        var form = ValidForm(); form.FirstName = new string('a', 100);
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(ContactUsForm.FirstName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EmptyLastName_Fails(string value)
    {
        var form = ValidForm(); form.LastName = value;
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ContactUsForm.LastName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("notanemail")]
    [InlineData("@nodomain")]
    [InlineData("missing-at-sign.com")]
    [InlineData("missing@tld")]
    [InlineData("two@@signs.com")]
    [InlineData("spaces in@example.com")]
    public async Task InvalidEmail_Fails(string email)
    {
        var form = ValidForm(); form.EmailAddress = email;
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ContactUsForm.EmailAddress));
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user+tag@domain.co.uk")]
    [InlineData("first.last@company.org")]
    [InlineData("katie@katiesgarden.uk")]
    public async Task ValidEmail_Passes(string email)
    {
        var form = ValidForm(); form.EmailAddress = email;
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(ContactUsForm.EmailAddress));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("abc")]
    public async Task InvalidPhone_Fails(string phone)
    {
        var form = ValidForm(); form.ContactNumber = phone;
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ContactUsForm.ContactNumber));
    }

    [Theory]
    [InlineData("07800 123456")]       // UK mobile with space
    [InlineData("07800123456")]        // UK mobile no space
    [InlineData("+447800123456")]      // International UK no space
    [InlineData("+44 7800 123456")]    // International UK with spaces
    [InlineData("01823 123456")]       // UK landline
    public async Task ValidPhone_Passes(string phone)
    {
        var form = ValidForm(); form.ContactNumber = phone;
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(ContactUsForm.ContactNumber));
    }

    [Fact]
    public async Task EmptySubject_Fails()
    {
        var form = ValidForm(); form.EmailSubject = "";
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ContactUsForm.EmailSubject));
    }

    [Fact]
    public async Task SubjectOver100Chars_Fails()
    {
        var form = ValidForm(); form.EmailSubject = new string('a', 101);
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ContactUsForm.EmailSubject));
    }

    [Fact]
    public async Task SubjectExactly100Chars_Passes()
    {
        var form = ValidForm(); form.EmailSubject = new string('a', 100);
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(ContactUsForm.EmailSubject));
    }

    [Fact]
    public async Task EmptyBody_Fails()
    {
        var form = ValidForm(); form.EmailBody = "";
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ContactUsForm.EmailBody));
    }

    [Fact]
    public async Task BodyOver2000Chars_Fails()
    {
        var form = ValidForm(); form.EmailBody = new string('a', 2001);
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ContactUsForm.EmailBody));
    }

    [Fact]
    public async Task BodyExactly2000Chars_Passes()
    {
        var form = ValidForm(); form.EmailBody = new string('a', 2000);
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().NotContain(e => e.PropertyName == nameof(ContactUsForm.EmailBody));
    }

    [Fact]
    public async Task EmailExceeding254Chars_Fails()
    {
        var form = ValidForm(); form.EmailAddress = new string('a', 250) + "@b.com";
        var result = await _validator.ValidateAsync(form, TestContext.Current.CancellationToken);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ContactUsForm.EmailAddress));
    }
}
