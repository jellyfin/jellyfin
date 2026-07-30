using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.SystemBackupService;
using MediaBrowser.Controller;
using MediaBrowser.Controller.SystemBackupService;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Creates a daily database backup, including CustomNetflix PostgreSQL when configured.
/// </summary>
public sealed class FullSystemBackupTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly IBackupService _backupService;
    private readonly IServerApplicationPaths _applicationPaths;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FullSystemBackupTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FullSystemBackupTask"/> class.
    /// </summary>
    /// <param name="backupService">The system backup service.</param>
    /// <param name="applicationPaths">The server application paths.</param>
    /// <param name="configuration">The server configuration.</param>
    /// <param name="logger">The logger.</param>
    public FullSystemBackupTask(
        IBackupService backupService,
        IServerApplicationPaths applicationPaths,
        IConfiguration configuration,
        ILogger<FullSystemBackupTask> logger)
    {
        _backupService = backupService;
        _applicationPaths = applicationPaths;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Database backup";

    /// <inheritdoc />
    public string Description => "Backs up Jellyfin and the CustomNetflix PostgreSQL data, then applies retention.";

    /// <inheritdoc />
    public string Category => "Maintenance";

    /// <inheritdoc />
    public string Key => "FullSystemDatabaseBackup";

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.DailyTrigger,
            TimeOfDayTicks = TimeSpan.FromHours(3).Ticks
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress.Report(5);
        var backup = await _backupService.CreateBackupAsync(new BackupOptionsDto
        {
            Database = true
        }).ConfigureAwait(false);

        progress.Report(80);
        // Local retention must not depend on optional off-host storage.
        ApplyRetention(_applicationPaths.BackupPath);

        var mirrorPath = _configuration["CustomNetflix:BackupMirrorPath"];
        if (!string.IsNullOrWhiteSpace(mirrorPath))
        {
            MirrorBackup(backup.Path, mirrorPath);
            ApplyRetention(mirrorPath);
        }

        progress.Report(100);
    }

    private void MirrorBackup(string backupPath, string mirrorPath)
    {
        var fullMirrorPath = Path.GetFullPath(mirrorPath);
        Directory.CreateDirectory(fullMirrorPath);
        var destination = Path.Combine(fullMirrorPath, Path.GetFileName(backupPath));
        var temporaryDestination = destination + ".partial";
        try
        {
            File.Copy(backupPath, temporaryDestination, true);
            File.Move(temporaryDestination, destination, true);
        }
        finally
        {
            File.Delete(temporaryDestination);
        }
    }

    private void ApplyRetention(string directory)
    {
        var retentionDays = Math.Max(1, _configuration.GetValue("CustomNetflix:BackupRetentionDays", 14));
        if (!Directory.Exists(directory))
        {
            return;
        }

        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        foreach (var path in Directory.EnumerateFiles(directory, "jellyfin-backup-*.zip", SearchOption.TopDirectoryOnly)
                     .Where(path => File.GetLastWriteTimeUtc(path) < cutoff))
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unable to delete expired backup {Path}", path);
            }
        }
    }
}
