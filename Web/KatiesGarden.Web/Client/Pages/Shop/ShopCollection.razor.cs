using KatiesGarden.Models.Shop;
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
    private string _sort = "featured";
    private bool _inStockOnly;

    private IEnumerable<ProductSummaryDto> SortedProducts
    {
        get
        {
            IEnumerable<ProductSummaryDto> source = _collection?.Products ?? [];
            if (_inStockOnly) source = source.Where(p => p.IsAvailable);
            return _sort switch
            {
                "price_asc"  => source.OrderBy(p => p.Price),
                "price_desc" => source.OrderByDescending(p => p.Price),
                "name"       => source.OrderBy(p => p.Name),
                _            => source.OrderBy(p => p.DisplayOrder),
            };
        }
    }

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
