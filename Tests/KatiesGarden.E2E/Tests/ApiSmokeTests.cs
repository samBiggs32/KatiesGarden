namespace KatiesGarden.E2E.Tests;

[TestFixture]
public class ApiSmokeTests : PlaywrightTestBase
{
    [Test]
    public async Task ShopCollections_Returns200()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/api/shop/collections");
        Assert.That(response.Status, Is.EqualTo(200), "GET /api/shop/collections should return 200");
    }

    [Test]
    public async Task ShopDeliverySettings_Returns200()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/api/shop/delivery-settings");
        Assert.That(response.Status, Is.EqualTo(200), "GET /api/shop/delivery-settings should return 200");
    }
}
