using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Migrations.Stages;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Generates default profile images for users who have no profile image,
/// or whose profile image file no longer exists on disk.
/// </summary>
[JellyfinMigration("2026-04-30T00:00:00", nameof(GenerateDefaultProfileImages), Stage = JellyfinMigrationStageTypes.AppInitialisation)]
internal class GenerateDefaultProfileImages : IAsyncMigrationRoutine
{
    private readonly IUserManager _userManager;
    private readonly IProfileImageService _profileImageService;
    private readonly ILogger<GenerateDefaultProfileImages> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateDefaultProfileImages"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="profileImageService">The profile image service.</param>
    /// <param name="logger">The logger.</param>
    public GenerateDefaultProfileImages(
        IUserManager userManager,
        IProfileImageService profileImageService,
        ILogger<GenerateDefaultProfileImages> logger)
    {
        _userManager = userManager;
        _profileImageService = profileImageService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var users = _userManager.Users;
        int generated = 0;
        int skipped = 0;

        foreach (var user in users)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var needsImage = user.ProfileImage is null
                || !System.IO.File.Exists(user.ProfileImage.Path);

            if (!needsImage)
            {
                skipped++;
                continue;
            }

            try
            {
                await _profileImageService.GenerateAndSaveProfileImageAsync(user).ConfigureAwait(false);
                generated++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate profile image for user {UserId}", user.Id);
            }
        }

        _logger.LogInformation(
            "Profile image generation complete: {Generated} generated, {Skipped} already had an image.",
            generated,
            skipped);
    }
}
