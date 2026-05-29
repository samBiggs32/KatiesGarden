using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace KatiesGarden.Web.Client.Pages.Shop;

public partial class ShopCollection : ComponentBase
{
    [Inject] ShopService ShopService { get; set; } = null!;
    [Inject] CartService CartService { get; set; } = null!;
    [Inject] ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public string Slug { get; set; } = string.Empty;

    private CollectionDetailDto? _collection;
    private bool _loading = true;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _collection = await ShopService.GetCollectionAsync(Slug);
        _loading = false;
    }

    private async Task AddToCartAsync(ProductSummaryDto product)
    {
        await CartService.AddAsync(new CartItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = 1,
            ImageUrl = product.CoverImageUrl,
            CanLocalDeliver = product.CanLocalDeliver
        });
        Snackbar.Add($"{product.Name} added to basket", Severity.Success);
    }
}
