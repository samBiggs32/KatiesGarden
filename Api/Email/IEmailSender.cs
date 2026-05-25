using KatiesGarden.Models.Email;

namespace KatiesGarden.Api.Email;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
