namespace KatiesGarden.Api.Helpers;

/// <summary>
/// Validates that an uploaded file's leading bytes ("magic number") match its declared
/// content type. The Content-Type header is attacker-controlled, so a header check alone
/// lets a malicious file (e.g. a script renamed to .jpg) reach blob storage where the CDN
/// serves it directly. Checking the actual bytes closes that gap.
/// </summary>
public static class ImageSignature
{
    /// <summary>
    /// Returns true if <paramref name="content"/> begins with a signature consistent with
    /// <paramref name="contentType"/>. Unknown content types return false (fail closed).
    /// </summary>
    public static bool Matches(ReadOnlySpan<byte> content, string contentType) => contentType switch
    {
        "image/jpeg" => StartsWith(content, [0xFF, 0xD8, 0xFF]),
        "image/png"  => StartsWith(content, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
        "image/gif"  => StartsWith(content, "GIF87a"u8) || StartsWith(content, "GIF89a"u8),
        // WebP: "RIFF" .... "WEBP" — bytes 0-3 and 8-11
        "image/webp" => content.Length >= 12
                        && StartsWith(content, "RIFF"u8)
                        && content.Slice(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };

    private static bool StartsWith(ReadOnlySpan<byte> content, ReadOnlySpan<byte> prefix) =>
        content.Length >= prefix.Length && content[..prefix.Length].SequenceEqual(prefix);
}
