using KatiesGarden.Models.Entities;
using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace KatiesGarden.Web.Client.Pages.Shop;

public partial class CheckoutPage : ComponentBase
{
    [Inject] CartService CartService { get; set; } = null!;
    [Inject] CheckoutService CheckoutService { get; set; } = null!;
    [Inject] ShopService ShopService { get; set; } = null!;
    [Inject] NavigationManager Navigation { get; set; } = null!;

    private static readonly CheckoutRequestValidator Validator = new();

    [SupplyParameterFromQuery] public string? Delivery { get; set; }

    private CheckoutRequest _request = new();
    private List<CartItem> _cartItems = [];
    private DeliverySettingsDto? _deliverySettings;
    private DeliveryType _deliveryType = DeliveryType.Collection;
    private bool _submitting;
    private string? _error;
    private Dictionary<string, string> _fieldErrors = [];

    private decimal _subtotal => _cartItems.Sum(i => i.LineTotal);
    private decimal _deliveryFee => _deliveryType == DeliveryType.LocalDelivery && _deliverySettings is not null
        ? (_deliverySettings.FreeDeliveryThreshold.HasValue && _subtotal >= _deliverySettings.FreeDeliveryThreshold.Value
            ? 0m
            : _deliverySettings.LocalDeliveryFee)
        : 0m;

    protected override async Task OnInitializedAsync()
    {
        _deliveryType = Enum.TryParse<DeliveryType>(Delivery, out var dt) ? dt : DeliveryType.Collection;
        _request.DeliveryType = _deliveryType;

        _cartItems = (await CartService.GetItemsAsync()).ToList();
        if (_cartItems.Count == 0) Navigation.NavigateTo("/cart");

        _deliverySettings = await ShopService.GetDeliverySettingsAsync();
    }

    private bool HasError(string field) => _fieldErrors.ContainsKey(field);
    private string GetError(string field) => _fieldErrors.TryGetValue(field, out var msg) ? msg : string.Empty;

    private async Task SubmitAsync()
    {
        _fieldErrors = [];
        _error = null;

        _request.Items = _cartItems
            .Select(i => new CartItemRequest { ProductId = i.ProductId, Quantity = i.Quantity })
            .ToList();

        var result = await Validator.ValidateAsync(_request);
        if (!result.IsValid)
        {
            _fieldErrors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.First().ErrorMessage);
            return;
        }

        _submitting = true;

        var url = await CheckoutService.CreateSessionAsync(_request);
        if (url is null)
        {
            _error = "There was a problem starting checkout. Please try again.";
            _submitting = false;
            return;
        }

        await CartService.ClearAsync();
        Navigation.NavigateTo(url, forceLoad: true);
    }
}
