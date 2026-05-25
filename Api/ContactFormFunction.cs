using FluentValidation;
using KatiesGarden.Api.Email;
using KatiesGarden.Models;
using KatiesGarden.Models.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace KatiesGarden.Api;

public class ContactFormFunction
{
    private readonly ILogger _logger;
    private readonly IValidator<ContactUsForm> _validator;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;

    public ContactFormFunction(
        ILoggerFactory loggerFactory,
        IValidator<ContactUsForm> validator,
        IEmailSender emailSender,
        IConfiguration config)
    {
        _logger = loggerFactory.CreateLogger<ContactFormFunction>();
        _validator = validator;
        _emailSender = emailSender;
        _config = config;
    }

    [Function("ContactForm")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;

        ContactUsForm? request;
        try
        {
            request = await req.ReadFromJsonAsync<ContactUsForm>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialise contact form request");
            return await Responses.BadRequest(req, "Invalid request body.");
        }

        if (request is null)
            return await Responses.BadRequest(req, "Request body is required.");

        var validation = await _validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return await Responses.BadRequest(req, validation.Errors.First().ErrorMessage);

        try
        {
            var senderEmail = _config["SENDER_EMAIL"]
                ?? _config["SMTP_USERNAME"]
                ?? throw new InvalidOperationException("SENDER_EMAIL or SMTP_USERNAME must be set");
            var recipientEmail = _config["RECIPIENT_EMAIL"]
                ?? throw new InvalidOperationException("RECIPIENT_EMAIL must be set");

            var message = ContactEmailBuilder.Build(request, senderEmail, recipientEmail);
            await _emailSender.SendAsync(message, ct);

            _logger.LogInformation("Contact form email sent from {FirstName} {LastName}", request.FirstName, request.LastName);
            return req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact form email");
            return await Responses.InternalError(req, "Failed to send message. Please try again later.");
        }
    }
}
