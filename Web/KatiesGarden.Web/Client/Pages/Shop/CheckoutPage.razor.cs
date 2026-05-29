using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace KatiesGarden.Web.Client.Pages.Shop;

public partial class CheckoutPage : ComponentBase
{
    [Inject] CartService CartService { get; set; } = null!;
    [Inject] CheckoutService CheckoutService { get; set; } = null!;
    [Inject] NavigationManager Navigation { get; set; } = null!;

    [SupplyParameterFromQuery] public string? Delivery { get; set; }

    private MudForm _form = null!;
    private CheckoutRequest _request = new();
    private string _deliveryType = "Collection";
    private bool _submitting;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        _deliveryType = Delivery ?? "Collection";
        _request.DeliveryType = _deliveryType;

        var items = await CartService.GetItemsAsync();
        if (items.Count == 0) Navigation.NavigateTo("/cart");
    }

    private async Task SubmitAsync()
    {
        await _form.Validate();
        if (!_form.IsValid) return;

        _submitting = true;
        _error = null;

        var items = await CartService.GetItemsAsync();
        _request.Items = items.Select(i => new CartItemRequest { ProductId = i.ProductId, Quantity = i.Quantity }).ToList();

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
