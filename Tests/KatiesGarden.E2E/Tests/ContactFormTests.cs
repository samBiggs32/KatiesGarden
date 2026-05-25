namespace KatiesGarden.E2E.Tests;

[TestFixture]
public class ContactFormTests : PlaywrightTestBase
{
    [SetUp]
    public async Task NavigateToContact()
    {
        await Page.GotoAsync($"{BaseUrl}/contact");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Test]
    public async Task AllFormFields_AreVisible()
    {
        await Expect(Page.GetByLabel("First Name")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Last Name")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Email Address")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Phone Number")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Subject")).ToBeVisibleAsync();
        await Expect(Page.GetByLabel("Message")).ToBeVisibleAsync();
    }

    [Test]
    public async Task EmptySubmit_ShowsValidationErrors()
    {
        await Page.ClickAsync("button:has-text('Send Message')");
        // MudBlazor renders validation errors with the mud-input-error class
        await Expect(Page.Locator(".mud-input-error").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task InvalidEmail_ShowsEmailValidationError()
    {
        await Page.GetByLabel("First Name").FillAsync("John");
        await Page.GetByLabel("Last Name").FillAsync("Smith");
        await Page.GetByLabel("Email Address").FillAsync("not-an-email");
        await Page.GetByLabel("Email Address").BlurAsync();
        await Expect(Page.Locator(".mud-input-error").First).ToBeVisibleAsync();
    }

    [Test]
    public async Task SubjectPrefilled_FromQueryString()
    {
        await Page.GotoAsync($"{BaseUrl}/contact?subject=Custom+Woodwork+Enquiry");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Expect(Page.GetByLabel("Subject")).ToHaveValueAsync("Custom Woodwork Enquiry");
    }

    [Test]
    public async Task ResetButton_ClearsForm()
    {
        await Page.GetByLabel("First Name").FillAsync("Jane");
        await Page.GetByLabel("Last Name").FillAsync("Doe");
        await Page.ClickAsync("button:has-text('Reset')");
        await Expect(Page.GetByLabel("First Name")).ToHaveValueAsync("");
    }

    [Test]
    public async Task LocationSection_ShowsMap()
    {
        await Expect(Page.Locator("text=Milverton")).ToBeVisibleAsync();
        await Expect(Page.Locator("img[alt*='Location']")).ToBeVisibleAsync();
    }
}
