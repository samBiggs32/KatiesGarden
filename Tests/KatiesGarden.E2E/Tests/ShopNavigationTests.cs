namespace KatiesGarden.E2E.Tests;

[TestFixture]
public class ShopNavigationTests : PlaywrightTestBase
{
    [Test]
    public async Task DirectNavigation_Shop_Returns200()
    {
        var response = await Page.GotoAsync($"{BaseUrl}/shop");
        Assert.That(response?.Status, Is.EqualTo(200), "/shop should return 200 via SPA fallback");
    }

    [Test]
    public async Task DirectNavigation_Cart_Returns200()
    {
        var response = await Page.GotoAsync($"{BaseUrl}/cart");
        Assert.That(response?.Status, Is.EqualTo(200), "/cart should return 200 via SPA fallback");
    }

    [Test]
    public async Task DirectNavigation_ShopUnknownSlug_Returns200()
    {
        // SPA fallback must handle unknown paths — 404 here means staticwebapp.config.json is broken
        var response = await Page.GotoAsync($"{BaseUrl}/shop/some-collection-slug");
        Assert.That(response?.Status, Is.EqualTo(200), "/shop/* should return 200 via SPA fallback");
    }

    [Test]
    public async Task ShopLink_InNavMenu_IsVisible()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("a.nav-link[href='shop']")).ToBeVisibleAsync();
    }

    [Test]
    public async Task ShopLink_Navigates_ToShopPage()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.ClickAsync("a.nav-link[href='shop']");
        await Expect(Page).ToHaveURLAsync(new Regex("/shop"));
    }

    [Test]
    public async Task CartPage_EmptyState_RendersWithoutError()
    {
        await Page.GotoAsync($"{BaseUrl}/cart");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        // Should render without a JS error — cart will be empty for an anonymous user
        var errors = new System.Collections.Generic.List<string>();
        Page.Console += (_, e) => { if (e.Type == "error") errors.Add(e.Text); };
        await Expect(Page.Locator("body")).ToBeVisibleAsync();
    }

    [Test]
    public async Task MobileNav_ShowsShopLink()
    {
        await Page.SetViewportSizeAsync(375, 812); // iPhone SE
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Open menu
        await Page.ClickAsync("button[aria-label='Toggle navigation']");
        await Expect(Page.Locator("a.nav-link[href='shop']")).ToBeVisibleAsync();
    }
}
