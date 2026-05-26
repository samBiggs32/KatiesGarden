using Azure.Storage.Blobs;
using KatiesGarden.Api.Auth;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace KatiesGarden.Api.Functions;

public class AdminImageFunction(BlobServiceClient? blobClient, IConfiguration config, ILogger<AdminImageFunction> logger)
{
    private static readonly HashSet<string> AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp", "image/gif"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    [Function("UploadImage")]
    public async Task<HttpResponseData> Upload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "admin/images")] HttpRequestData req)
    {
        if (!SwaAuth.IsAdmin(req))
            return req.CreateResponse(HttpStatusCode.Unauthorized);

        if (blobClient is null)
        {
            logger.LogError("Blob storage not configured — AZURE_STORAGE_CONNECTION_STRING missing");
            return req.CreateResponse(HttpStatusCode.ServiceUnavailable);
        }

        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : string.Empty;

        if (!AllowedContentTypes.Any(a => contentType.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Only JPEG, PNG, WebP, and GIF images are accepted.");
            return bad;
        }

        if (req.Body.Length > MaxFileSizeBytes)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Image must be 5 MB or smaller.");
            return bad;
        }

        var extension = contentType switch
        {
            var s when s.Contains("jpeg") => ".jpg",
            var s when s.Contains("png") => ".png",
            var s when s.Contains("webp") => ".webp",
            _ => ".jpg"
        };
        var blobName = $"{Guid.NewGuid()}{extension}";
        var containerName = config["AZURE_STORAGE_CONTAINER"] ?? "product-images";

        var container = blobClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(req.Body, overwrite: false, req.FunctionContext.CancellationToken);

        logger.LogInformation("Uploaded image {BlobName}", blobName);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { url = blob.Uri.ToString() });
        return response;
    }
}
