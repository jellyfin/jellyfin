using System;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.StartupUiPreview;

/// <summary>
/// Builds a representative startup log tree so the preview exercises nesting and every severity icon.
/// </summary>
internal static class SampleData
{
    // Fixed base time so the rendered output is stable between refreshes.
    private static readonly DateTimeOffset _base = new(2026, 6, 21, 14, 30, 0, TimeSpan.Zero);

    /// <summary>
    /// Builds the sample log topics shown in the preview.
    /// </summary>
    /// <param name="includeError">When true, appends a critical failure topic to preview the error styling.</param>
    /// <returns>An array of root level <see cref="StartupLogTopic"/>.</returns>
    public static StartupLogTopic[] BuildSampleLog(bool includeError)
    {
        var seconds = 0;
        StartupLogTopic Topic(LogLevel level, string content)
            => new() { LogLevel = level, Content = content, DateOfCreation = _base.AddSeconds(seconds++) };

        var storage = Topic(LogLevel.None, "Storage Check");
        storage.Children.Add(Topic(LogLevel.None, "Validated data path: /config (412 GiB free)"));
        storage.Children.Add(Topic(LogLevel.None, "Validated cache path: /cache (412 GiB free)"));
        storage.Children.Add(Topic(LogLevel.None, "Validated transcode path: /cache/transcodes"));

        var migrations = Topic(LogLevel.Information, "Applying database migrations");
        migrations.Children.Add(Topic(LogLevel.None, "20240403_AddCustomDisplayPreferences applied"));
        migrations.Children.Add(Topic(LogLevel.None, "20240517_MoveTrickplayFiles applied"));
        var slowMigration = Topic(LogLevel.Warning, "20240901_ReindexLibrary applied");
        slowMigration.Children.Add(Topic(LogLevel.Warning, "Reindex took 42s, longer than expected for large libraries"));
        migrations.Children.Add(slowMigration);

        var services = Topic(LogLevel.Information, "Initializing core services");
        services.Children.Add(Topic(LogLevel.None, "Loaded 7 plugins"));
        services.Children.Add(Topic(LogLevel.Information, "Network configuration loaded"));

        var roots = new System.Collections.Generic.List<StartupLogTopic> { storage, migrations, services };

        if (includeError)
        {
            var failure = Topic(LogLevel.Critical, "Failed to bind HTTP listener");
            failure.Children.Add(Topic(LogLevel.Error, "Address http://0.0.0.0:8096 is already in use"));
            roots.Add(failure);
        }

        return roots.ToArray();
    }
}
