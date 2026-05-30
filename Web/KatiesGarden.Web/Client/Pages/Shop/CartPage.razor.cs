using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Models;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace KatiesGarden.Web.Client.Pages.Shop;

public partial class CartPage : ComponentBase, IDisposable
{
    [Inject] CartService CartService { get; set; } = null!;
    [Inject] ShopService ShopService { get; set; } = null!;
    [Inject] CheckoutService CheckoutService { get; set; } = null!;
    [Inject] NavigationManager Navigation { get; set; } = null!;

    private List<CartItem> _items = [];
    private DeliverySettingsDto? _deliverySettings;
    private string _deliveryType = "Collection";
    private bool _checkingOut;

    private decimal _subtotal => _items.Sum(i => i.LineTotal);
    private bool _allCanDeliver => _items.All(i => i.CanLocalDeliver);
    private decimal _deliveryFee => _deliveryType == "LocalDelivery" && _deliverySettings is not null
        ? (_deliverySettings.FreeDeliveryThreshold.HasValue && _subtotal >= _deliverySettings.FreeDeliveryThreshold.Value
            ? 0m
            : _deliverySettings.LocalDeliveryFee)
        : 0m;

    private string DeliveryFeeLabel => _deliverySettings is null
        ? "..."
        : (_deliverySettings.FreeDeliveryThreshold.HasValue
            ? $"£{_deliverySettings.LocalDeliveryFee:F2} (free over £{_deliverySettings.FreeDeliveryThreshold:F2})"
            : $"£{_deliverySettings.LocalDeliveryFee:F2}");

    protected override async Task OnInitializedAsync()
    {
        _items = (await CartService.GetItemsAsync()).ToList();
        _deliverySettings = await ShopService.GetDeliverySettingsAsync();
        if (!_allCanDeliver) _deliveryType = "Collection";
        CartService.CartChanged += OnCartChanged;
    }

    // Synchronous event handler — dispatches async work back to the component's
    // render context via InvokeAsync so StateHasChanged is always called on the
    // UI thread, and any exceptions surface in the component's error boundary.
    private void OnCartChanged()
    {
        _ = InvokeAsync(async () =>
        {
            _items = (await CartService.GetItemsAsync()).ToList();
            StateHasChanged();
        });
    }

    private async Task UpdateQuantityAsync(Guid productId, int quantity)
    {
        await CartService.UpdateQuantityAsync(productId, quantity);
    }

    private async Task RemoveAsync(Guid productId)
    {
        await CartService.RemoveAsync(productId);
    }

    private async Task ProceedToCheckoutAsync()
    {
        if (_items.Count == 0) return;
        _checkingOut = true;

        // For now, navigate to checkout detail page where customer enters their info
        Navigation.NavigateTo($"/checkout?delivery={_deliveryType}");
    }

    private void SelectCollection(Microsoft.AspNetCore.Components.ChangeEventArgs _) => _deliveryType = "Collection";
    private void SelectLocalDelivery(Microsoft.AspNetCore.Components.ChangeEventArgs _) => _deliveryType = "LocalDelivery";

    public void Dispose()
    {
        CartService.CartChanged -= OnCartChanged;
    }
}
