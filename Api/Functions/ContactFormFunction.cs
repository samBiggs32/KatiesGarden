using FluentValidation;
using KatiesGarden.Api.Configuration;
using KatiesGarden.Api.Email;
using KatiesGarden.Api.Helpers;
using KatiesGarden.Models;
using KatiesGarden.Models.Email;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace KatiesGarden.Api.Functions;

public class ContactFormFunction(
    IValidator<ContactUsForm> validator,
    IEmailSender emailSender,
    IOptions<SmtpOptions> smtpOptions,
    ILogger<ContactFormFunction> logger)
{
    [Function("ContactForm")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequestData req)
    {
        var ct = req.FunctionContext.CancellationToken;

        ContactUsForm? request;
        try { request = await req.ReadFromJsonAsync<ContactUsForm>(); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deserialise contact form request");
            return await Responses.BadRequest(req, "Invalid request body.");
        }

        if (request is null)
            return await Responses.BadRequest(req, "Request body is required.");

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            logger.LogInformation("Contact form validation failed: {Errors}",
                string.Join(", ", validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));
            return await Responses.BadRequest(req, validation.Errors.First().ErrorMessage);
        }

        try
        {
            var smtp = smtpOptions.Value;
            var message = ContactEmailBuilder.Build(request, smtp.EffectiveSenderEmail, smtp.RecipientEmail);
            await emailSender.SendAsync(message, ct);

            logger.LogInformation("Contact form email sent from {EmailHash}", LogRedaction.Hash(request.EmailAddress));
            return req.CreateResponse(HttpStatusCode.OK);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send contact form email from {EmailHash}", LogRedaction.Hash(request.EmailAddress));
            return await Responses.InternalError(req, "Failed to send message. Please try again later.");
        }
    }
}
