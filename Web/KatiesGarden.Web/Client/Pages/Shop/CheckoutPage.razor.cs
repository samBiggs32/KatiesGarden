using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;

namespace KatiesGarden.Web.Client.Pages.Shop;

public partial class CheckoutPage : ComponentBase
{
    [Inject] CartService CartService { get; set; } = null!;
    [Inject] CheckoutService CheckoutService { get; set; } = null!;
    [Inject] NavigationManager Navigation { get; set; } = null!;

    [SupplyParameterFromQuery] public string? Delivery { get; set; }

    private static readonly CheckoutRequestValidator _validator = new();

    private CheckoutRequest _request = new();
    private string _deliveryType = "Collection";
    private bool _submitting;
    private string? _error;
    private Dictionary<string, string> _fieldErrors = [];

    protected override async Task OnInitializedAsync()
    {
        _deliveryType = Delivery ?? "Collection";
        _request.DeliveryType = _deliveryType;

        var items = await CartService.GetItemsAsync();
        if (items.Count == 0) Navigation.NavigateTo("/cart");
    }

    private async Task SubmitAsync()
    {
        _error = null;
        _fieldErrors = [];

        var items = await CartService.GetItemsAsync();
        _request.Items = items.Select(i => new CartItemRequest { ProductId = i.ProductId, Quantity = i.Quantity }).ToList();
        _request.DeliveryType = _deliveryType;

        var result = _validator.Validate(_request);
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

    private bool HasError(string field) => _fieldErrors.ContainsKey(field);
    private string GetError(string field) => _fieldErrors.TryGetValue(field, out var msg) ? msg : string.Empty;
}
