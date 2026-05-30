using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace KatiesGarden.Web.Client.Pages.Shop;

public partial class ShopProduct : ComponentBase, IDisposable
{
    [Inject] ShopService ShopService { get; set; } = null!;
    [Inject] CartService CartService { get; set; } = null!;

    [Parameter] public string Slug { get; set; } = string.Empty;

    private ProductDetailDto? _product;
    private bool _loading = true;
    private int _activeImage;
    private string? _toast;
    private System.Threading.Timer? _toastTimer;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _activeImage = 0;
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
        ShowToast($"{_product.Name} added to basket");
    }

    private void ShowToast(string message)
    {
        _toast = message;
        _toastTimer?.Dispose();
        _toastTimer = new System.Threading.Timer(_ =>
            InvokeAsync(() => { _toast = null; StateHasChanged(); }),
            null, 3000, Timeout.Infinite);
    }

    public void Dispose() => _toastTimer?.Dispose();
}
