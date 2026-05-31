using FluentAssertions;
using KatiesGarden.Api.Helpers;
using Xunit;

namespace KatiesGarden.Tests.Helpers;

public class ImageSignatureTests
{
    // Minimal valid magic-byte headers for each supported type
    private static byte[] Jpeg()   => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x01];
    private static byte[] Png()    => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
    private static byte[] Gif87()  => "GIF87a"u8.ToArray();
    private static byte[] Gif89()  => "GIF89a"u8.ToArray();
    private static byte[] WebP()   => [.. "RIFF"u8, 0x00, 0x00, 0x00, 0x00, .. "WEBP"u8];

    [Fact]
    public void Matches_JpegBytes_JpegContentType_ReturnsTrue() =>
        ImageSignature.Matches(Jpeg(), "image/jpeg").Should().BeTrue();

    [Fact]
    public void Matches_PngBytes_PngContentType_ReturnsTrue() =>
        ImageSignature.Matches(Png(), "image/png").Should().BeTrue();

    [Fact]
    public void Matches_Gif87Bytes_GifContentType_ReturnsTrue() =>
        ImageSignature.Matches(Gif87(), "image/gif").Should().BeTrue();

    [Fact]
    public void Matches_Gif89Bytes_GifContentType_ReturnsTrue() =>
        ImageSignature.Matches(Gif89(), "image/gif").Should().BeTrue();

    [Fact]
    public void Matches_WebpBytes_WebpContentType_ReturnsTrue() =>
        ImageSignature.Matches(WebP(), "image/webp").Should().BeTrue();

    [Fact]
    public void Matches_JpegBytesWithWrongContentType_ReturnsFalse() =>
        ImageSignature.Matches(Jpeg(), "image/png").Should().BeFalse();

    [Fact]
    public void Matches_PngBytesWithWrongContentType_ReturnsFalse() =>
        ImageSignature.Matches(Png(), "image/jpeg").Should().BeFalse();

    [Fact]
    public void Matches_ScriptBytes_JpegContentType_ReturnsFalse()
    {
        // A PHP/script file renamed to .jpg — typical attacker vector
        var script = "<?php echo 'evil'; ?>"u8.ToArray();
        ImageSignature.Matches(script, "image/jpeg").Should().BeFalse();
    }

    [Fact]
    public void Matches_UnknownContentType_ReturnsFalse() =>
        ImageSignature.Matches(Jpeg(), "image/tiff").Should().BeFalse();

    [Fact]
    public void Matches_EmptyContent_ReturnsFalse() =>
        ImageSignature.Matches([], "image/jpeg").Should().BeFalse();

    [Fact]
    public void Matches_WebpTooShort_ReturnsFalse()
    {
        // Only 8 bytes — can't contain WEBP marker at offset 8
        byte[] tooShort = [.. "RIFF"u8, 0x00, 0x00, 0x00, 0x00];
        ImageSignature.Matches(tooShort, "image/webp").Should().BeFalse();
    }
}
