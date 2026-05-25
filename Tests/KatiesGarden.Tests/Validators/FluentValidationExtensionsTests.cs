using FluentAssertions;
using KatiesGarden.Models;
using KatiesGarden.Models.Validators;
using Xunit;

namespace KatiesGarden.Tests.Validators;

public class FluentValidationExtensionsTests
{
    private readonly ContactUsFormValidator _validator = new();

    private static ContactUsForm AllInvalidForm() => new()
    {
        FirstName = "",
        LastName = "",
        EmailAddress = "not-an-email",
        ContactNumber = "abc",
        EmailSubject = "",
        EmailBody = ""
    };

    [Fact]
    public async Task ToFieldValidator_OnlyReturnsErrorsForRequestedField()
    {
        var fieldValidator = _validator.ToFieldValidator();
        var form = AllInvalidForm();

        var firstNameErrors = (await fieldValidator(form, nameof(ContactUsForm.FirstName))).ToList();
        firstNameErrors.Should().NotBeEmpty();
        // Should not include error messages for EmailAddress, ContactNumber etc.
        firstNameErrors.Should().NotContain(m => m.Contains("phone", StringComparison.OrdinalIgnoreCase));
        firstNameErrors.Should().NotContain(m => m.Contains("email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ToFieldValidator_ReturnsEmpty_WhenFieldIsValid()
    {
        var fieldValidator = _validator.ToFieldValidator();
        var form = AllInvalidForm();
        form.FirstName = "Katie"; // make only this field valid

        var firstNameErrors = await fieldValidator(form, nameof(ContactUsForm.FirstName));
        firstNameErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task ToFieldValidator_OtherFieldsStillReportTheirErrors()
    {
        var fieldValidator = _validator.ToFieldValidator();
        var form = AllInvalidForm();
        form.FirstName = "Katie"; // fix only first name

        // Email is still invalid, querying email should still surface its errors
        var emailErrors = await fieldValidator(form, nameof(ContactUsForm.EmailAddress));
        emailErrors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ToFieldValidator_UnknownProperty_ReturnsEmpty()
    {
        var fieldValidator = _validator.ToFieldValidator();
        var form = AllInvalidForm();

        var errors = await fieldValidator(form, "NotAProperty");
        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ToFieldValidator_ReturnsErrorMessages_NotPropertyNames()
    {
        var fieldValidator = _validator.ToFieldValidator();
        var form = AllInvalidForm();
        form.EmailAddress = "missing@tld";  // valid-looking but fails the regex

        var errors = (await fieldValidator(form, nameof(ContactUsForm.EmailAddress))).ToList();
        errors.Should().Contain(m => m.Contains("valid email", StringComparison.OrdinalIgnoreCase));
    }
}
