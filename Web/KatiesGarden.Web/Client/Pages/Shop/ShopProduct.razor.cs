using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace KatiesGarden.Web.Client.Pages.Shop;

public partial class ShopProduct : ComponentBase
{
    [Inject] ShopService ShopService { get; set; } = null!;
    [Inject] CartService CartService { get; set; } = null!;
    [Inject] ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public string Slug { get; set; } = string.Empty;

    private ProductDetailDto? _product;
    private bool _loading = true;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _product = await ShopService.GetProductAsync(Slug);
        _loading = false;
    }

    private async Task AddToCartAsync()
    {
        if (_product is null) return;
        await CartService.AddAsync(new CartItem
        {
            ProductId = _product.Id,
            ProductName = _product.Name,
            UnitPrice = _product.Price,
            Quantity = 1,
            ImageUrl = _product.ImageUrls.FirstOrDefault(),
            CanLocalDeliver = _product.CanLocalDeliver
        });
        Snackbar.Add($"{_product.Name} added to basket", Severity.Success);
    }
}
