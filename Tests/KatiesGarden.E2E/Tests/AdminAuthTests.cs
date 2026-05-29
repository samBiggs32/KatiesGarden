namespace KatiesGarden.E2E.Tests;

[TestFixture]
public class AdminAuthTests : PlaywrightTestBase
{
    [Test]
    public async Task AdminRoute_Unauthenticated_RedirectsToAuthOrLogin()
    {
        // SWA's route rule requires "admin" role on /admin.
        // Unauthenticated users are redirected to /.auth/login or /admin/login.
        await Page.GotoAsync($"{BaseUrl}/admin");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var url = Page.Url;
        var isRedirected = url.Contains(".auth/login") || url.Contains("/admin/login");
        Assert.That(isRedirected, Is.True, $"Expected redirect to login page, but got URL: {url}");
    }

    [Test]
    public async Task DirectNavigation_AdminLogin_Returns200()
    {
        var response = await Page.GotoAsync($"{BaseUrl}/admin/login");
        Assert.That(response?.Status, Is.EqualTo(200), "/admin/login should return 200");
    }

    [Test]
    public async Task AdminLoginPage_HasGitHubButton()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("text=Continue with GitHub")).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminLoginPage_HasGoogleButton()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("text=Continue with Google")).ToBeVisibleAsync();
    }

    [Test]
    public async Task AdminLoginPage_HasMicrosoftButton()
    {
        await Page.GotoAsync($"{BaseUrl}/admin/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("text=Continue with Microsoft")).ToBeVisibleAsync();
    }
}
