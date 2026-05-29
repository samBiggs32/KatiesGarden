using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "manage/images")] HttpRequestData req)
    {
        if (!SwaAuth.IsAdmin(req))
            return req.CreateResponse(HttpStatusCode.Unauthorized);

        if (blobClient is null)
        {
            logger.LogError("Blob storage not configured — AZURE_STORAGE_CONNECTION_STRING missing");
            return req.CreateResponse(HttpStatusCode.ServiceUnavailable);
        }

        var contentType = req.Headers.TryGetValues("Content-Type", out var ctHeaders) ? ctHeaders.First() : string.Empty;
        // Normalise: strip any charset/boundary parameters
        var bareContentType = contentType.Split(';', 2)[0].Trim();

        if (!AllowedContentTypes.Contains(bareContentType.ToLowerInvariant()))
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync("Only JPEG, PNG, WebP, and GIF images are accepted.");
            return bad;
        }

        // Buffer the upload so we can enforce the size limit before sending to blob storage
        var ct = req.FunctionContext.CancellationToken;
        using var buffer = new MemoryStream();
        var readBuffer = new byte[8192];
        int read;
        while ((read = await req.Body.ReadAsync(readBuffer, ct)) > 0)
        {
            if (buffer.Length + read > MaxFileSizeBytes)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Image must be 5 MB or smaller.");
                return bad;
            }
            await buffer.WriteAsync(readBuffer.AsMemory(0, read), ct);
        }
        buffer.Position = 0;

        var extension = bareContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };
        var blobName = $"{Guid.NewGuid()}{extension}";
        var containerName = config["AZURE_STORAGE_CONTAINER"] ?? "product-images";

        var container = blobClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        await blob.UploadAsync(
            buffer,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = bareContentType } },
            ct);

        logger.LogInformation("Uploaded image {BlobName}", blobName);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new ImageUploadResponse(blob.Uri.ToString()));
        return response;
    }

    public record ImageUploadResponse(string Url);
}
