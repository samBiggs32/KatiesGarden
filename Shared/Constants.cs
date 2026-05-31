namespace KatiesGarden.Models;

public static class Constants
{
    // Maximum image upload size enforced by both the API (AdminImageFunction) and client-side
    // file pickers (ProductEditor, CollectionEditor). Keeping them in sync here means a single
    // change propagates to all three places.
    public const long MaxImageFileSizeBytes = 5L * 1024 * 1024; // 5 MB
}
