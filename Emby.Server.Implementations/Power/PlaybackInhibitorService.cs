using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Power;

/// <summary>
/// Maintains system power inhibitors during active playback.
/// </summary>
public sealed class PlaybackInhibitorService : IHostedService, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly ILogger<PlaybackInhibitorService> _logger;
    private readonly PlaybackInhibitor _inhibitor;

    private readonly Lock _syncLock = new();
    private readonly HashSet<string> _activeSessions = new(StringComparer.Ordinal);

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackInhibitorService"/> class.
    /// </summary>
    public PlaybackInhibitorService(
        ISessionManager sessionManager,
        IServerConfigurationManager configurationManager,
        ILogger<PlaybackInhibitorService> logger)
    {
        _sessionManager = sessionManager;
        _configurationManager = configurationManager;
        _logger = logger;
        _inhibitor = new PlaybackInhibitor(logger);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        _sessionManager.SessionEnded += OnSessionEnded;
        _configurationManager.ConfigurationUpdated += OnConfigurationUpdated;

        SeedFromActiveSessions();
        UpdateInhibitorState();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _sessionManager.SessionEnded -= OnSessionEnded;
        _configurationManager.ConfigurationUpdated -= OnConfigurationUpdated;

        lock (_syncLock)
        {
            _activeSessions.Clear();
        }

        _inhibitor.Disable();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _inhibitor.Dispose();
    }

    private void OnConfigurationUpdated(object? sender, EventArgs e)
        => UpdateInhibitorState();

    private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e)
    {
        if (e.Session?.Id is null)
        {
            return;
        }

        lock (_syncLock)
        {
            _activeSessions.Add(e.Session.Id);
        }

        UpdateInhibitorState();
    }

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        if (e.Session?.Id is null)
        {
            return;
        }

        lock (_syncLock)
        {
            _activeSessions.Remove(e.Session.Id);
        }

        UpdateInhibitorState();
    }

    private void OnSessionEnded(object? sender, SessionEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Session?.Id))
        {
            return;
        }

        lock (_syncLock)
        {
            _activeSessions.Remove(e.Session.Id);
        }

        UpdateInhibitorState();
    }

    private void SeedFromActiveSessions()
    {
        lock (_syncLock)
        {
            foreach (var session in _sessionManager.Sessions)
            {
                if (session?.NowPlayingItem is not null)
                {
                    _activeSessions.Add(session.Id);
                }
            }
        }
    }

    private void UpdateInhibitorState()
    {
        if (!_configurationManager.Configuration.EnablePlaybackInhibitors)
        {
            _inhibitor.Disable();
            return;
        }

        bool hasActivePlayback;
        lock (_syncLock)
        {
            hasActivePlayback = _activeSessions.Count > 0;
        }

        if (hasActivePlayback)
        {
            _inhibitor.Enable();
        }
        else
        {
            _inhibitor.Disable();
        }
    }

    private sealed class PlaybackInhibitor : IDisposable
    {
        private readonly ILogger _logger;

        private Process? _linuxInhibitorProcess;
        private uint _macAssertionId;
        private bool _enabled;

        public PlaybackInhibitor(ILogger logger)
        {
            _logger = logger;
        }

        public void Enable()
        {
            if (_enabled)
            {
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                _logger.LogInformation("Enabling playback inhibitors on Windows.");
                _enabled = EnableWindows();
                return;
            }

            if (OperatingSystem.IsLinux())
            {
                _logger.LogInformation("Enabling playback inhibitors on Linux using systemd-inhibit.");
                _enabled = EnableLinux();
                return;
            }

            if (OperatingSystem.IsMacOS())
            {
                _logger.LogInformation("Enabling playback inhibitors on macOS using IOPMAssertion.");
                _enabled = EnableMacOS();
                return;
            }

            _logger.LogWarning("Playback inhibitors are only supported on Windows, Linux, and macOS.");
        }

        public void Disable()
        {
            if (!_enabled)
            {
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                DisableWindows();
            }
            else if (OperatingSystem.IsLinux())
            {
                DisableLinux();
            }
            else if (OperatingSystem.IsMacOS())
            {
                DisableMacOS();
            }

            _enabled = false;
        }

        public void Dispose()
        {
            DisableLinux();
            DisableMacOS();
        }

        private bool EnableLinux()
        {
            if (_linuxInhibitorProcess is { HasExited: false })
            {
                return true;
            }

            try
            {
                const string systemdInhibitPath = "/usr/bin/systemd-inhibit";
                if (!File.Exists(systemdInhibitPath))
                {
                    _logger.LogWarning("systemd-inhibit was not found at {Path}; playback inhibitors are not active.", systemdInhibitPath);
                    return false;
                }

                const string sleepPath = "/usr/bin/sleep";
                if (!File.Exists(sleepPath))
                {
                    _logger.LogWarning("sleep was not found at {Path}; playback inhibitors are not active.", sleepPath);
                    return false;
                }

                var startInfo = new ProcessStartInfo(systemdInhibitPath)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                startInfo.ArgumentList.Add("--what=shutdown:sleep");
                startInfo.ArgumentList.Add("--mode=block");
                startInfo.ArgumentList.Add("--who=Jellyfin");
                startInfo.ArgumentList.Add("--why=Playback active");
                startInfo.ArgumentList.Add("--");
                startInfo.ArgumentList.Add(sleepPath);
                startInfo.ArgumentList.Add("infinity");

                _linuxInhibitorProcess = Process.Start(startInfo);

                if (_linuxInhibitorProcess is null)
                {
                    _logger.LogWarning("Failed to start systemd-inhibit for playback inhibitors.");
                    return false;
                }

                if (_linuxInhibitorProcess.HasExited)
                {
                    _logger.LogWarning("systemd-inhibit exited immediately, playback inhibitors are not active.");
                    _linuxInhibitorProcess.Dispose();
                    _linuxInhibitorProcess = null;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to enable systemd-inhibit playback inhibitors.");
                return false;
            }
        }

        private void DisableLinux()
        {
            if (_linuxInhibitorProcess is null)
            {
                return;
            }

            try
            {
                if (!_linuxInhibitorProcess.HasExited)
                {
                    _linuxInhibitorProcess.Kill(entireProcessTree: true);
                    _linuxInhibitorProcess.WaitForExit(TimeSpan.FromSeconds(2));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error stopping systemd-inhibit playback inhibitors.");
            }
            finally
            {
                _linuxInhibitorProcess.Dispose();
                _linuxInhibitorProcess = null;
            }
        }

        [Flags]
        private enum ExecutionState : uint
        {
            ES_CONTINUOUS = 0x80000000,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        [DllImport("kernel32.dll")]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        private bool EnableWindows()
        {
            var result = SetThreadExecutionState(ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED);
            if (result == 0)
            {
                _logger.LogWarning("Unable to enable playback sleep inhibitor on Windows.");
                return false;
            }

            return true;
        }

        private static void DisableWindows()
            => SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);

        private const uint KIOReturnSuccess = 0;
        private const string PowerManagementFramework = "/System/Library/Frameworks/IOKit.framework/IOKit";

        private enum IOPMAssertionType : uint
        {
            NoIdleSleep = 0,
            NoDisplaySleep = 1,
            NoDiskIdle = 2,
            NoSystemSleep = 3
        }

        [DllImport(PowerManagementFramework)]
        private static extern uint IOPMAssertionCreateWithName(
            IOPMAssertionType assertionType,
            uint assertionLevel,
            string assertionName,
            out uint assertionId);

        [DllImport(PowerManagementFramework)]
        private static extern uint IOPMAssertionRelease(uint assertionId);

        private bool EnableMacOS()
        {
            if (_macAssertionId != 0)
            {
                return true;
            }

            var result = IOPMAssertionCreateWithName(
                IOPMAssertionType.NoIdleSleep,
                1,
                "Jellyfin Playback",
                out var assertionId);

            if (result != KIOReturnSuccess)
            {
                _logger.LogWarning("Unable to enable playback sleep inhibitor on macOS (result {Result}).", result);
                return false;
            }

            _macAssertionId = assertionId;
            return true;
        }

        private void DisableMacOS()
        {
            if (_macAssertionId == 0)
            {
                return;
            }

            try
            {
                var result = IOPMAssertionRelease(_macAssertionId);
                if (result != KIOReturnSuccess)
                {
                    _logger.LogDebug("Unable to release macOS playback inhibitor (result {Result}).", result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error releasing macOS playback inhibitor.");
            }
            finally
            {
                _macAssertionId = 0;
            }
        }
    }
}
