using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace KatiesGarden.Web.Client.Pages.Shop;

public partial class ShopCollection : ComponentBase, IDisposable
{
    [Inject] ShopService ShopService { get; set; } = null!;
    [Inject] CartService CartService { get; set; } = null!;

    [Parameter] public string Slug { get; set; } = string.Empty;

    private CollectionDetailDto? _collection;
    private bool _loading = true;
    private string? _toast;
    private System.Threading.Timer? _toastTimer;

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
        ShowToast($"{product.Name} added to basket");
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
