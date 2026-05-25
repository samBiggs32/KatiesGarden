namespace KatiesGarden.E2E.Tests;

[TestFixture]
public class GalleryTests : PlaywrightTestBase
{
    [SetUp]
    public async Task NavigateToGallery()
    {
        await Page.GotoAsync($"{BaseUrl}/gallery");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Test]
    public async Task ThreeTabs_AreAllVisible()
    {
        await Expect(Page.Locator(".mud-tab:has-text('Woodwork')")).ToBeVisibleAsync();
        await Expect(Page.Locator(".mud-tab:has-text('Bugs and Hugs')")).ToBeVisibleAsync();
        await Expect(Page.Locator(".mud-tab:has-text('Maintenance')")).ToBeVisibleAsync();
    }

    [Test]
    public async Task WoodworkTab_ShowsCarousel()
    {
        await Page.ClickAsync(".mud-tab:has-text('Woodwork')");
        await Expect(Page.Locator(".gallery-carousel, .mud-carousel").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task BugsAndHugsTab_ShowsCarousel()
    {
        await Page.ClickAsync(".mud-tab:has-text('Bugs and Hugs')");
        await Expect(Page.Locator(".gallery-carousel, .mud-carousel").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task RequestWoodwork_NavigatesToContactWithSubject()
    {
        await Page.ClickAsync("button:has-text('Request Custom Woodwork')");
        await Expect(Page).ToHaveURLAsync(new Regex(@"/contact\?subject=.*[Ww]oodwork"));
    }

    [Test]
    public async Task OrderBugHouse_NavigatesToContactWithSubject()
    {
        await Page.ClickAsync(".mud-tab:has-text('Bugs and Hugs')");
        await Page.ClickAsync("button:has-text('Order Custom Bug House')");
        await Expect(Page).ToHaveURLAsync(new Regex(@"/contact\?subject=.*[Bb]ug"));
    }
}
