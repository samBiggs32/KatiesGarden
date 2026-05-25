namespace KatiesGarden.E2E.Tests;

[TestFixture]
public class NavigationTests : PlaywrightTestBase
{
    /// <summary>
    /// Direct URL access tests — these verify the staticwebapp.config.json SPA
    /// fallback is configured correctly. A 404 here means the config is broken.
    /// </summary>
    [Test]
    public async Task DirectNavigation_Gallery_Loads()
    {
        var response = await Page.GotoAsync($"{BaseUrl}/gallery");
        Assert.That(response?.Status, Is.EqualTo(200), "/gallery should return 200 via SPA fallback");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("text=Our Gallery")).ToBeVisibleAsync();
    }

    [Test]
    public async Task DirectNavigation_Contact_Loads()
    {
        var response = await Page.GotoAsync($"{BaseUrl}/contact");
        Assert.That(response?.Status, Is.EqualTo(200), "/contact should return 200 via SPA fallback");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("text=Get in Touch")).ToBeVisibleAsync();
    }

    [Test]
    public async Task NavLink_Gallery_NavigatesAndShowsTabs()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.ClickAsync("a.nav-link[href='gallery']");
        await Expect(Page).ToHaveURLAsync(new Regex("/gallery"));
        await Expect(Page.Locator(".mud-tabs")).ToBeVisibleAsync();
    }

    [Test]
    public async Task NavLink_Contact_NavigatesAndShowsForm()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.ClickAsync("a.nav-link[href='contact']");
        await Expect(Page).ToHaveURLAsync(new Regex("/contact"));
        await Expect(Page.Locator("text=Get in Touch")).ToBeVisibleAsync();
    }

    [Test]
    public async Task NavLink_Home_NavigatesBackToHome()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.ClickAsync("a.nav-link[href='']");
        await Expect(Page).ToHaveURLAsync(new Regex(@"^https?://[^/]+(/#?)?$"));
    }

    [Test]
    public async Task MobileNav_TogglesOpenAndClosed()
    {
        await Page.SetViewportSizeAsync(375, 812); // iPhone SE
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var navLinks = Page.Locator("a.nav-link");
        // Nav should be collapsed on mobile
        await Expect(navLinks.First).ToBeHiddenAsync();

        // Open the menu
        await Page.ClickAsync("button[aria-label='Toggle navigation']");
        await Expect(navLinks.First).ToBeVisibleAsync();

        // Close the menu
        await Page.ClickAsync("button[aria-label='Toggle navigation']");
        await Expect(navLinks.First).ToBeHiddenAsync();
    }
}
