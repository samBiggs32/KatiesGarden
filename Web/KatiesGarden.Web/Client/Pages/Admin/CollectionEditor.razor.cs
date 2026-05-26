using KatiesGarden.Models.Shop;
using KatiesGarden.Web.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace KatiesGarden.Web.Client.Pages.Admin;

public partial class CollectionEditor
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

    private string _title = string.Empty;
    private string _description = string.Empty;
    private string? _coverImageUrl;
    private bool _isActive = true;
    private int _displayOrder;
    private DateTime? _startDate = DateTime.Today;
    private DateTime? _endDate;
    private bool _hasEndDate;

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;

        if (!_isNew)
        {
            var collection = await AdminProductService.GetCollectionAsync(Id!.Value);
            if (collection is null)
            {
                Navigation.NavigateTo("/admin/collections");
                return;
            }

            _title = collection.Title;
            _description = collection.Description;
            _coverImageUrl = collection.CoverImageUrl;
            _isActive = collection.IsActive;
            _displayOrder = collection.DisplayOrder;
            _startDate = collection.StartDate;
            _hasEndDate = collection.EndDate.HasValue;
            _endDate = collection.EndDate;
        }

        _loading = false;
    }

    private async Task TriggerCoverUpload()
        => await JS.InvokeVoidAsync("clickElement", "cover-upload");

    private async Task OnCoverImageSelected(InputFileChangeEventArgs e)
    {
        _uploadError = string.Empty;
        _uploading = true;

        const long maxSize = 5 * 1024 * 1024;
        var file = e.File;

        if (file.Size > maxSize)
        {
            _uploadError = $"{file.Name} exceeds the 5 MB limit.";
            _uploading = false;
            return;
        }

        var contentType = file.ContentType is "image/jpeg" or "image/png" or "image/webp"
            ? file.ContentType
            : "image/jpeg";

        await using var stream = file.OpenReadStream(maxSize);
        var url = await AdminProductService.UploadImageAsync(stream, contentType);
        if (url is not null)
            _coverImageUrl = url;
        else
            _uploadError = $"Failed to upload {file.Name}.";

        _uploading = false;
        StateHasChanged();
    }

    private async Task SaveAsync()
    {
        _saveError = string.Empty;

        if (string.IsNullOrWhiteSpace(_title))
        {
            _saveError = "Collection title is required.";
            return;
        }

        if (_hasEndDate && _endDate.HasValue && _startDate.HasValue && _endDate <= _startDate)
        {
            _saveError = "End date must be after start date.";
            return;
        }

        _saving = true;

        if (_isNew)
        {
            var request = new CreateCollectionRequest
            {
                Title = _title.Trim(),
                Description = _description.Trim(),
                CoverImageUrl = _coverImageUrl,
                StartDate = _startDate?.ToUniversalTime() ?? DateTime.UtcNow,
                EndDate = _hasEndDate ? _endDate?.ToUniversalTime() : null,
                DisplayOrder = _displayOrder
            };

            var result = await AdminProductService.CreateCollectionAsync(request);
            if (result is not null)
            {
                Snackbar.Add("Collection created", Severity.Success);
                Navigation.NavigateTo("/admin/collections");
            }
            else
            {
                _saveError = "Failed to create collection. Please try again.";
            }
        }
        else
        {
            var request = new UpdateCollectionRequest
            {
                Title = _title.Trim(),
                Description = _description.Trim(),
                CoverImageUrl = _coverImageUrl,
                IsActive = _isActive,
                StartDate = _startDate?.ToUniversalTime() ?? DateTime.UtcNow,
                EndDate = _hasEndDate ? _endDate?.ToUniversalTime() : null,
                DisplayOrder = _displayOrder
            };

            var result = await AdminProductService.UpdateCollectionAsync(Id!.Value, request);
            if (result is not null)
            {
                Snackbar.Add("Collection saved", Severity.Success);
                Navigation.NavigateTo("/admin/collections");
            }
            else
            {
                _saveError = "Failed to save collection. Please try again.";
            }
        }

        _saving = false;
    }
}
