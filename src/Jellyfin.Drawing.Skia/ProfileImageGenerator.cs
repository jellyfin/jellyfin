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
    // A palette of pleasant background colors
    private static readonly SKColor[] _colorPalette =
    [
        new SKColor(0xE5, 0x39, 0x35), // Red
        new SKColor(0xD8, 0x1B, 0x60), // Pink
        new SKColor(0x8E, 0x24, 0xAA), // Purple
        new SKColor(0x39, 0x49, 0xAB), // Indigo
        new SKColor(0x1E, 0x88, 0xE5), // Blue
        new SKColor(0x00, 0x89, 0x7B), // Teal
        new SKColor(0x43, 0xA0, 0x47), // Green
        new SKColor(0xF4, 0x51, 0x1E), // Deep Orange
        new SKColor(0x6D, 0x4C, 0x41), // Brown
        new SKColor(0x00, 0xAC, 0xC1), // Cyan
        new SKColor(0x7C, 0xB3, 0x42), // Light Green
        new SKColor(0xFF, 0xB3, 0x00), // Amber
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
        canvas.Clear(SKColors.Transparent);

        // Draw circular background
        using var circlePaint = new SKPaint
        {
            Color = backgroundColor,
            IsAntialias = true
        };
        canvas.DrawCircle(ImageSize / 2f, ImageSize / 2f, ImageSize / 2f, circlePaint);

        // Draw initials text
        var typeface = SKTypeface.FromFamilyName(null, SKFontStyle.Bold);
        using var font = new SKFont(typeface, TextSize);
        using var textPaint = new SKPaint
        {
            Color = SKColors.White,
            IsAntialias = true
        };

        font.MeasureText(initials, out SKRect textBounds, textPaint);

        var textX = ((ImageSize - textBounds.Width) / 2f) - textBounds.Left;
        var textY = ((ImageSize - textBounds.Height) / 2f) - textBounds.Top;
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
