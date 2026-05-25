using FluentAssertions;
using KatiesGarden.Models;
using KatiesGarden.Models.Email;
using Xunit;

namespace KatiesGarden.Tests.Email;

public class ContactEmailBuilderTests
{
    private static ContactUsForm SampleForm() => new()
    {
        FirstName = "Katie",
        LastName = "Porter",
        EmailAddress = "katie@example.com",
        ContactNumber = "07800 123456",
        EmailSubject = "Hedge trimming quote",
        EmailBody = "Hello, please could you quote for trimming our hedges."
    };

    [Fact]
    public void Build_UsesSenderAsFromAddress()
    {
        var msg = ContactEmailBuilder.Build(SampleForm(), "noreply@katiesgarden.uk", "team@katiesgarden.uk");
        msg.FromAddress.Should().Be("noreply@katiesgarden.uk");
        msg.FromName.Should().Be(ContactEmailBuilder.FromName);
    }

    [Fact]
    public void Build_UsesRecipientAsToAddress()
    {
        var msg = ContactEmailBuilder.Build(SampleForm(), "noreply@katiesgarden.uk", "team@katiesgarden.uk");
        msg.ToAddress.Should().Be("team@katiesgarden.uk");
        msg.ToName.Should().Be(ContactEmailBuilder.ToName);
    }

    [Fact]
    public void Build_SetsReplyToToFormSubmitter()
    {
        var msg = ContactEmailBuilder.Build(SampleForm(), "noreply@katiesgarden.uk", "team@katiesgarden.uk");
        msg.ReplyToAddress.Should().Be("katie@example.com");
        msg.ReplyToName.Should().Be("Katie Porter");
    }

    [Fact]
    public void Build_PrefixesSubjectWithWebsiteEnquiry()
    {
        var msg = ContactEmailBuilder.Build(SampleForm(), "noreply@katiesgarden.uk", "team@katiesgarden.uk");
        msg.Subject.Should().StartWith(ContactEmailBuilder.SubjectPrefix);
        msg.Subject.Should().Contain("Hedge trimming quote");
    }

    [Fact]
    public void Build_BodyContainsAllFormData()
    {
        var msg = ContactEmailBuilder.Build(SampleForm(), "noreply@katiesgarden.uk", "team@katiesgarden.uk");
        msg.BodyText.Should().Contain("Hello, please could you quote for trimming our hedges.");
        msg.BodyText.Should().Contain("Katie Porter");
        msg.BodyText.Should().Contain("katie@example.com");
        msg.BodyText.Should().Contain("07800 123456");
    }

    [Fact]
    public void Build_BodyOpensWithDearKatie()
    {
        var msg = ContactEmailBuilder.Build(SampleForm(), "x", "y");
        msg.BodyText.Should().StartWith("Dear Katie,");
    }

    [Theory]
    [InlineData("'; DROP TABLE Subscribers; --", "harmless")]
    [InlineData("<script>alert(1)</script>", "harmless")]
    [InlineData("Plain text", "with\nnewlines\nand\ttabs")]
    public void Build_PreservesFormContentVerbatim_NoEscaping(string subject, string body)
    {
        // Plain-text emails: content is rendered as-is. No HTML interpretation,
        // no SQL execution — verify the builder doesn't strip or modify input.
        var form = SampleForm();
        form.EmailSubject = subject;
        form.EmailBody = body;
        var msg = ContactEmailBuilder.Build(form, "from@x.com", "to@x.com");
        msg.Subject.Should().Contain(subject);
        msg.BodyText.Should().Contain(body);
    }
}
