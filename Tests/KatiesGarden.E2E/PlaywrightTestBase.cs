namespace KatiesGarden.E2E;

/// <summary>
/// Base class for all Playwright tests. Set PLAYWRIGHT_BASE_URL to target a
/// specific environment; defaults to production.
/// </summary>
public abstract class PlaywrightTestBase : PageTest
{
    protected string BaseUrl =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL")?.TrimEnd('/')
        ?? "https://www.katiesgarden.uk";
}
