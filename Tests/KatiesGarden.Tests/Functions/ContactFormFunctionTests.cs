using FluentAssertions;
using KatiesGarden.Api;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using Xunit;

namespace KatiesGarden.Tests.Functions;

public class ContactFormFunctionTests
{
    private static ContactFormFunction CreateSut()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        return new ContactFormFunction(loggerFactory);
    }

    private static FakeHttpRequestData CreateRequest(object? body) =>
        new(Substitute.For<FunctionContext>(), body);

    private static ContactFormRequest ValidRequest() => new()
    {
        FirstName = "John",
        LastName = "Smith",
        EmailAddress = "john@example.com",
        ContactNumber = "07800 123456",
        EmailSubject = "Garden enquiry",
        EmailBody = "I would like to learn more about your services."
    };

    [Fact]
    public async Task NullBody_ReturnsBadRequest()
    {
        var result = await CreateSut().Run(CreateRequest(null));
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("FirstName")]
    [InlineData("LastName")]
    [InlineData("EmailAddress")]
    [InlineData("ContactNumber")]
    [InlineData("EmailSubject")]
    [InlineData("EmailBody")]
    public async Task MissingRequiredField_ReturnsBadRequest(string fieldName)
    {
        var req = ValidRequest();
        typeof(ContactFormRequest).GetProperty(fieldName)!.SetValue(req, "");

        var result = await CreateSut().Run(CreateRequest(req));

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhitespaceOnlyFields_ReturnsBadRequest()
    {
        var req = ValidRequest(); req.FirstName = "   ";
        var result = await CreateSut().Run(CreateRequest(req));
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AllFieldsPresent_NoSmtpConfigured_Returns500()
    {
        // Valid input reaches SendEmailAsync which throws because SMTP env vars
        // are not set in the test environment. The catch block returns 500.
        // This validates the error handling path and confirms all-valid input
        // passes the guard clauses.
        var result = await CreateSut().Run(CreateRequest(ValidRequest()));
        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}
