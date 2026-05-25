namespace KatiesGarden.E2E.Tests;

[TestFixture]
public class HomePageTests : PlaywrightTestBase
{
    [Test]
    public async Task PageTitle_ContainsKatiesGarden()
    {
        await Page.GotoAsync(BaseUrl);
        await Expect(Page).ToHaveTitleAsync(new Regex("Katie", RegexOptions.IgnoreCase));
    }

    [Test]
    public async Task HeroCarousel_IsVisible()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator(".mud-carousel")).ToBeVisibleAsync();
    }

    [Test]
    public async Task CompanyDescription_IsVisible()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("text=Katie's Garden")).First.ToBeVisibleAsync();
    }

    [Test]
    public async Task GetInTouch_CTA_IsVisible()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.Locator("a[href='/contact'], a[href='contact']").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task PhoneNumber_IsVisibleInNav()
    {
        await Page.GotoAsync(BaseUrl);
        await Expect(Page.Locator("text=07804 784522")).ToBeVisibleAsync();
    }
}
