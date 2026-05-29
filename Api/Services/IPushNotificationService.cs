namespace KatiesGarden.Api.Services;

public interface IPushNotificationService
{
    Task SendAsync(string title, string body, CancellationToken ct = default);
}
