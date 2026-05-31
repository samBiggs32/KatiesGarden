using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace KatiesGarden.Web.Client.Pages.Shop;

public partial class ShopIndex : ComponentBase
{
    [Inject] ShopService ShopService { get; set; } = null!;

    private List<CollectionSummaryDto>? _collections;
    private bool _loading = true;
    private bool _loadError;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _collections = await ShopService.GetCollectionsAsync();
        }
        catch
        {
            _loadError = true;
        }
        finally
        {
            _loading = false;
        }
    }
}
