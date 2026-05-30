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
    private int _quantity = 1;
    private string? _toast;
    private Timer? _toastTimer;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _activeImage = 0;
        _quantity = 1;
        _product = await ShopService.GetProductAsync(Slug);
        _loading = false;
    }

    private void IncrementQty()
    {
        if (_product?.StockQuantity.HasValue == true && _quantity >= _product.StockQuantity.Value) return;
        _quantity++;
    }

    private void DecrementQty()
    {
        if (_quantity > 1) _quantity--;
    }

    private async Task AddToCartAsync()
    {
        if (_product is null) return;
        await CartService.AddAsync(new CartItem
        {
            ProductId = _product.Id,
            ProductName = _product.Name,
            UnitPrice = _product.Price,
            Quantity = _quantity,
            ImageUrl = _product.ImageUrls.FirstOrDefault(),
            CanLocalDeliver = _product.CanLocalDeliver
        });
        ShowToast(_quantity > 1
            ? $"{_quantity}× {_product.Name} added to basket"
            : $"{_product.Name} added to basket");
        _quantity = 1;
    }

    private void ShowToast(string message)
    {
        _toast = message;
        _toastTimer?.Dispose();
        _toastTimer = new Timer(_ =>
        {
            _toast = null;
            _ = InvokeAsync(StateHasChanged);
        }, null, 3000, Timeout.Infinite);
        StateHasChanged();
    }

    public void Dispose() => _toastTimer?.Dispose();
}
