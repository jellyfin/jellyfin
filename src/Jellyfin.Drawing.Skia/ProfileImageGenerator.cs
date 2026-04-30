using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia;

/// <summary>
/// Generates a simple circular profile image containing the user's initials.
/// </summary>
public class ProfileImageGenerator : MediaBrowser.Controller.Drawing.IProfileImageService
{
    // A palette of muted background colors suitable for dark-theme UIs.
    // Colors are kept at moderate saturation (~50%) and mid-range brightness
    // so they remain clearly distinct without being eye-searing, while still
    // providing enough contrast for white initials to read comfortably.
    private static readonly SKColor[] _colorPalette =
    [
        new SKColor(0x9E, 0x40, 0x40), // Muted Crimson
        new SKColor(0x9E, 0x58, 0x30), // Muted Terra Cotta
        new SKColor(0x9E, 0x78, 0x30), // Muted Ochre
        new SKColor(0x7A, 0x8C, 0x30), // Muted Olive
        new SKColor(0x30, 0x8A, 0x40), // Muted Forest Green
        new SKColor(0x30, 0x8A, 0x6E), // Muted Jade
        new SKColor(0x2E, 0x78, 0x78), // Muted Teal
        new SKColor(0x30, 0x6A, 0x8E), // Muted Steel Blue
        new SKColor(0x30, 0x4E, 0x9E), // Muted Cobalt
        new SKColor(0x4E, 0x30, 0x9E), // Muted Indigo
        new SKColor(0x70, 0x30, 0x9E), // Muted Violet
        new SKColor(0x9E, 0x30, 0x80), // Muted Plum
        new SKColor(0x9E, 0x30, 0x50), // Muted Rose
        new SKColor(0x7A, 0x40, 0x40), // Muted Brick
        new SKColor(0x48, 0x7A, 0x8E), // Muted Cadet Blue
        new SKColor(0x48, 0x90, 0x6E), // Muted Sage
        new SKColor(0x60, 0x48, 0x90), // Muted Slate Purple
        new SKColor(0x90, 0x60, 0x48), // Muted Sienna
        new SKColor(0x40, 0x78, 0x58), // Muted Spruce
        new SKColor(0x78, 0x58, 0x40), // Muted Walnut
    ];

    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly IProviderManager _providerManager;
    private readonly ILogger<ProfileImageGenerator> _logger;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileImageGenerator"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">The server configuration manager.</param>
    /// <param name="providerManager">The provider manager.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="userManager">The user manager, used to persist profile image updates.</param>
    public ProfileImageGenerator(
        IServerConfigurationManager serverConfigurationManager,
        IProviderManager providerManager,
        ILogger<ProfileImageGenerator> logger,
        IUserManager userManager)
    {
        _serverConfigurationManager = serverConfigurationManager;
        _providerManager = providerManager;
        _logger = logger;
        _userManager = userManager;
    }

    /// <inheritdoc/>
    public async Task GenerateAndSaveProfileImageAsync(
        User user,
        string? displayName)
    {
        _logger.LogDebug("Generating profile image for user {UserId}", user.Id);

        if (user.ProfileImage is not null)
        {
            try
            {
                System.IO.File.Delete(user.ProfileImage.Path);
                _logger.LogDebug("Deleted previous profile image at {Path}", user.ProfileImage.Path);
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Failed to delete existing profile image at {Path}", user.ProfileImage.Path);
            }

            await _userManager.ClearProfileImageAsync(user).ConfigureAwait(false);
        }

        var userDataPath = Path.Combine(
                   _serverConfigurationManager.ApplicationPaths.UserConfigurationDirectoryPath,
                   user.Username);
        var imagePath = Path.Combine(userDataPath, "profile.png");

        var stream = await GenerateProfileImageAsync(displayName ?? user.Username, user.Id).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            await _providerManager.SaveImage(stream, "image/png", imagePath).ConfigureAwait(false);
        }

        user.ProfileImage = new ImageInfo(imagePath);
        await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
        _logger.LogDebug("Saved profile image for user {UserId} to {Path}", user.Id, imagePath);
    }

    /// <inheritdoc/>
    public Task<Stream> GenerateProfileImageAsync(string displayName, Guid userId)
    {
        var initials = GetInitials(displayName);
        var backgroundColor = PickColor(userId);

        const int ImageSize = 256;
        const int TextSize = 96;

        using var bitmap = new SKBitmap(ImageSize, ImageSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(backgroundColor);

        // Draw initials text
        var typeface = SKTypeface.FromFamilyName(null, SKFontStyle.Bold);
        using var font = new SKFont(typeface, TextSize);
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };

        // Measure the actual glyph bounds for accurate horizontal centering.
        font.MeasureText(initials, out SKRect textBounds);
        var textX = (ImageSize / 2f) - textBounds.MidX;

        // Use the font's cap height for vertical centering instead of per-glyph bounds.
        // Per-glyph bounds shift downward when diacritics are present (e.g. "Ö"),
        // whereas cap height is constant for all uppercase combinations.
        // Cap height is negative in Skia (measured above the baseline), so take its absolute value.
        var capHeight = Math.Abs(font.Metrics.CapHeight);
        var textY = capHeight > 0f
            ? (ImageSize / 2f) + (capHeight / 2f)
            : (ImageSize / 2f) - textBounds.MidY;

        canvas.DrawText(initials, textX, textY, font, textPaint);

        using var image = SKImage.FromBitmap(bitmap);
        using var encodedData = image.Encode(SKEncodedImageFormat.Png, 100);
        var memoryStream = new MemoryStream();
        encodedData.SaveTo(memoryStream);
        memoryStream.Position = 0;

        return Task.FromResult<Stream>(memoryStream);
    }

    /// <summary>
    /// Extracts up to two initials from a display name.
    /// </summary>
    private static string GetInitials(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return "?";
        }

        var parts = displayName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
        }

        var name = parts[0];
        return name.Length >= 2
            ? $"{char.ToUpperInvariant(name[0])}{char.ToUpperInvariant(name[1])}"
            : char.ToUpperInvariant(name[0]).ToString();
    }

    /// <summary>
    /// Deterministically picks a background color based on the user's ID.
    /// </summary>
    private static SKColor PickColor(Guid userId)
    {
        // Use a stable hash of the user ID so the same user always gets the same color
        var hash = Math.Abs(userId.GetHashCode());
        return _colorPalette[hash % _colorPalette.Length];
    }
}
