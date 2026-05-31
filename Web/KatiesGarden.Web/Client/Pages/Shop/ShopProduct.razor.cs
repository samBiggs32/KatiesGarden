using System.Text.Json.Nodes;
using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace KatiesGarden.Web.Client.Pages.Shop;

public partial class ShopProduct : ComponentBase
{
    [Inject] ShopService ShopService { get; set; } = null!;
    [Inject] CartService CartService { get; set; } = null!;

    [Parameter] public string Slug { get; set; } = string.Empty;

    private ProductDetailDto? _product;
    private string? _jsonLd;
    private bool _loading = true;
    private int _activeImage;
    private int _quantity = 1;
    private string? _toast;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        _activeImage = 0;
        _quantity = 1;
        _product = await ShopService.GetProductAsync(Slug);
        _jsonLd = _product is null ? null : BuildJsonLd(_product);
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
        _toast = _quantity > 1
            ? $"{_quantity}× {_product.Name} added to basket"
            : $"{_product.Name} added to basket";
        _quantity = 1;
    }

    private static string BuildJsonLd(ProductDetailDto p)
    {
        var obj = new JsonObject
        {
            ["@context"] = "https://schema.org/",
            ["@type"] = "Product",
            ["name"] = p.Name,
            ["description"] = p.Description,
            ["offers"] = new JsonObject
            {
                ["@type"] = "Offer",
                ["priceCurrency"] = "GBP",
                ["price"] = p.Price.ToString("F2"),
                ["availability"] = p.IsAvailable ? "https://schema.org/InStock" : "https://schema.org/OutOfStock",
                ["url"] = $"https://www.katiesgarden.uk/shop/product/{p.Slug}"
            },
            ["brand"] = new JsonObject
            {
                ["@type"] = "Brand",
                ["name"] = "Katie's Garden"
            }
        };
        if (p.ImageUrls.Length > 0)
            obj["image"] = p.ImageUrls[0];
        return obj.ToJsonString();
    }
}
