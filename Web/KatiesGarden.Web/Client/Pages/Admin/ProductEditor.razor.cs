using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace KatiesGarden.Web.Client.Pages.Admin;

public partial class ProductEditor
{
    [Parameter] public Guid? Id { get; set; }

    [Inject] AdminProductService AdminProductService { get; set; } = null!;
    [Inject] ISnackbar Snackbar { get; set; } = null!;
    [Inject] NavigationManager Navigation { get; set; } = null!;
    [Inject] IJSRuntime JS { get; set; } = null!;

    private bool _isNew => Id is null;
    private bool _loading = true;
    private bool _saving;
    private bool _uploading;
    private string _saveError = string.Empty;
    private string _uploadError = string.Empty;

    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _howToBuyNote = string.Empty;
    private decimal _price = 0.01m;
    private bool _isAvailable = true;
    private bool _canLocalDeliver = true;
    private bool _trackStock;
    private int? _stockQuantity;
    private Guid? _collectionId;
    private int _displayOrder;
    private List<string> _imageUrls = [];

    private List<CollectionSummaryDto> _collections = [];

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;

        var collectionsTask = AdminProductService.GetCollectionsAsync();

        if (!_isNew)
        {
            var product = await AdminProductService.GetProductAsync(Id!.Value);
            if (product is null)
            {
                Navigation.NavigateTo("/admin/products");
                return;
            }

            _name = product.Name;
            _description = product.Description;
            _howToBuyNote = product.HowToBuyNote ?? string.Empty;
            _price = product.Price;
            _isAvailable = product.IsAvailable;
            _canLocalDeliver = product.CanLocalDeliver;
            _trackStock = product.StockQuantity.HasValue;
            _stockQuantity = product.StockQuantity;
            _collectionId = product.CollectionId;
            _imageUrls = product.ImageUrls.ToList();
            _displayOrder = product.DisplayOrder;
        }

        _collections = await collectionsTask ?? [];
        _loading = false;
    }

    private void RemoveImage(int index)
    {
        if (index >= 0 && index < _imageUrls.Count)
            _imageUrls.RemoveAt(index);
    }

    private async Task TriggerFileInput()
        => await JS.InvokeVoidAsync("clickElement", "image-upload");

    private async Task OnImagesSelected(InputFileChangeEventArgs e)
    {
        _uploadError = string.Empty;
        _uploading = true;

        const long maxSize = 5 * 1024 * 1024;
        var remaining = 6 - _imageUrls.Count;
        var files = e.GetMultipleFiles(remaining);

        foreach (var file in files)
        {
            if (file.Size > maxSize)
            {
                _uploadError = $"{file.Name} exceeds the 5 MB limit.";
                continue;
            }

            var contentType = file.ContentType is "image/jpeg" or "image/png" or "image/webp"
                ? file.ContentType
                : "image/jpeg";

            await using var stream = file.OpenReadStream(maxSize);
            var url = await AdminProductService.UploadImageAsync(stream, contentType);
            if (url is not null)
                _imageUrls.Add(url);
            else
                _uploadError = $"Failed to upload {file.Name}.";
        }

        _uploading = false;
        StateHasChanged();
    }

    private async Task SaveAsync()
    {
        _saveError = string.Empty;

        if (string.IsNullOrWhiteSpace(_name))
        {
            _saveError = "Product name is required.";
            return;
        }

        if (_price <= 0)
        {
            _saveError = "Price must be greater than zero.";
            return;
        }

        _saving = true;

        if (_isNew)
        {
            var request = new CreateProductRequest
            {
                Name = _name.Trim(),
                Description = _description.Trim(),
                Price = _price,
                StockQuantity = _trackStock ? _stockQuantity : null,
                IsAvailable = _isAvailable,
                CanLocalDeliver = _canLocalDeliver,
                ImageUrls = _imageUrls.ToArray(),
                CollectionId = _collectionId,
                HowToBuyNote = string.IsNullOrWhiteSpace(_howToBuyNote) ? null : _howToBuyNote.Trim(),
                DisplayOrder = _displayOrder
            };

            var result = await AdminProductService.CreateProductAsync(request);
            if (result is not null)
            {
                Snackbar.Add("Product created", Severity.Success);
                Navigation.NavigateTo("/admin/products");
            }
            else
            {
                _saveError = "Failed to create product. Please try again.";
            }
        }
        else
        {
            var request = new UpdateProductRequest
            {
                Name = _name.Trim(),
                Description = _description.Trim(),
                Price = _price,
                StockQuantity = _trackStock ? _stockQuantity : null,
                IsAvailable = _isAvailable,
                CanLocalDeliver = _canLocalDeliver,
                ImageUrls = _imageUrls.ToArray(),
                CollectionId = _collectionId,
                HowToBuyNote = string.IsNullOrWhiteSpace(_howToBuyNote) ? null : _howToBuyNote.Trim(),
                DisplayOrder = _displayOrder
            };

            var result = await AdminProductService.UpdateProductAsync(Id!.Value, request);
            if (result is not null)
            {
                Snackbar.Add("Product saved", Severity.Success);
                Navigation.NavigateTo("/admin/products");
            }
            else
            {
                _saveError = "Failed to save product. Please try again.";
            }
        }

        _saving = false;
    }
}
