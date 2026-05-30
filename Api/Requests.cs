namespace KatiesGarden.Api;

// API-internal request/response records — not referenced by the Shared project
// or the client; only ever (de)serialised by this process via ReadFromJsonAsync
// / WriteAsJsonAsync.

internal record DeliverySettingsUpdateRequest(
    decimal LocalDeliveryFee,
    decimal? FreeDeliveryThreshold,
    string DeliveryAreaDescription,
    string CollectionAddress,
    string CollectionInstructions);

internal record AdminNotesRequest(string? Notes);

internal record ImageUploadResponse(string Url);
