using KatiesGarden.Models.Shop;
using Microsoft.JSInterop;
using System.Text.Json;

namespace KatiesGarden.Web.Client.Services;

public class CartService(IJSRuntime js)
{
    private List<CartItem> _items = [];
    private bool _loaded;

    public event Action? CartChanged;

    public async Task<IReadOnlyList<CartItem>> GetItemsAsync()
    {
        await EnsureLoadedAsync();
        return _items.AsReadOnly();
    }

    public int ItemCount => _items.Sum(i => i.Quantity);

    public async Task AddAsync(CartItem item)
    {
        await EnsureLoadedAsync();
        var existing = _items.FirstOrDefault(i => i.ProductId == item.ProductId);
        if (existing is not null)
            existing.Quantity += item.Quantity;
        else
            _items.Add(item);
        await PersistAsync();
        CartChanged?.Invoke();
    }

    public async Task UpdateQuantityAsync(Guid productId, int quantity)
    {
        await EnsureLoadedAsync();
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item is null) return;
        if (quantity <= 0)
            _items.Remove(item);
        else
            item.Quantity = quantity;
        await PersistAsync();
        CartChanged?.Invoke();
    }

    public async Task RemoveAsync(Guid productId)
    {
        await EnsureLoadedAsync();
        _items.RemoveAll(i => i.ProductId == productId);
        await PersistAsync();
        CartChanged?.Invoke();
    }

    public async Task ClearAsync()
    {
        _items.Clear();
        await js.InvokeVoidAsync("CartStorage.clear");
        CartChanged?.Invoke();
    }

    public decimal Subtotal => _items.Sum(i => i.LineTotal);

    private async Task EnsureLoadedAsync()
    {
        if (_loaded) return;
        try
        {
            var json = await js.InvokeAsync<string?>("CartStorage.get");
            if (!string.IsNullOrWhiteSpace(json))
                _items = JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
        }
        catch { _items = []; }
        _loaded = true;
    }

    private async Task PersistAsync()
    {
        var json = JsonSerializer.Serialize(_items);
        await js.InvokeVoidAsync("CartStorage.set", json);
    }
}
