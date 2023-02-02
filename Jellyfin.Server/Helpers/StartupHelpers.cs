using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using SQLitePCL;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Jellyfin.Server.Helpers;

/// <summary>
/// A class containing helper methods for server startup.
/// </summary>
public static class StartupHelpers
{
    private static readonly string[] _relevantEnvVarPrefixes = { "JELLYFIN_", "DOTNET_", "ASPNETCORE_" };

    /// <summary>
    /// Logs relevant environment variables and information about the host.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="appPaths">The application paths to use.</param>
    public static void LogEnvironmentInfo(ILogger logger, IApplicationPaths appPaths)
    {
        // Distinct these to prevent users from reporting problems that aren't actually problems
        var commandLineArgs = Environment
            .GetCommandLineArgs()
            .Distinct();

        // Get all relevant environment variables
        var allEnvVars = Environment.GetEnvironmentVariables();
        var relevantEnvVars = new Dictionary<object, object>();
        foreach (var key in allEnvVars.Keys)
        {
            if (_relevantEnvVarPrefixes.Any(prefix => key.ToString()!.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                relevantEnvVars.Add(key, allEnvVars[key]!);
            }
        }

        logger.LogInformation("Environment Variables: {EnvVars}", relevantEnvVars);
        logger.LogInformation("Arguments: {Args}", commandLineArgs);
        logger.LogInformation("Operating system: {OS}", RuntimeInformation.OSDescription);
        logger.LogInformation("Architecture: {Architecture}", RuntimeInformation.OSArchitecture);
        logger.LogInformation("64-Bit Process: {Is64Bit}", Environment.Is64BitProcess);
        logger.LogInformation("User Interactive: {IsUserInteractive}", Environment.UserInteractive);
        logger.LogInformation("Processor count: {ProcessorCount}", Environment.ProcessorCount);
        logger.LogInformation("Program data path: {ProgramDataPath}", appPaths.ProgramDataPath);
        logger.LogInformation("Web resources path: {WebPath}", appPaths.WebPath);
        logger.LogInformation("Application directory: {ApplicationPath}", appPaths.ProgramSystemPath);
    }

    /// <summary>
    /// Create the data, config and log paths from the variety of inputs(command line args,
    /// environment variables) or decide on what default to use. For Windows it's %AppPath%
    /// for everything else the
    /// <a href="https://specifications.freedesktop.org/basedir-spec/basedir-spec-latest.html">XDG approach</a>
    /// is followed.
    /// </summary>
    /// <param name="options">The <see cref="StartupOptions" /> for this instance.</param>
    /// <returns><see cref="ServerApplicationPaths" />.</returns>
    public static ServerApplicationPaths CreateApplicationPaths(StartupOptions options)
    {
        // dataDir
        // IF      --datadir
        // ELSE IF $JELLYFIN_DATA_DIR
        // ELSE IF windows, use <%APPDATA%>/jellyfin
        // ELSE IF $XDG_DATA_HOME then use $XDG_DATA_HOME/jellyfin
        // ELSE    use $HOME/.local/share/jellyfin
        var dataDir = options.DataDir;
        if (string.IsNullOrEmpty(dataDir))
        {
            dataDir = Environment.GetEnvironmentVariable("JELLYFIN_DATA_DIR");

            if (string.IsNullOrEmpty(dataDir))
            {
                // LocalApplicationData follows the XDG spec on unix machines
                dataDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "jellyfin");
            }
        }

        // configDir
        // IF      --configdir
        // ELSE IF $JELLYFIN_CONFIG_DIR
        // ELSE IF --datadir, use <datadir>/config (assume portable run)
        // ELSE IF <datadir>/config exists, use that
        // ELSE IF windows, use <datadir>/config
        // ELSE IF $XDG_CONFIG_HOME use $XDG_CONFIG_HOME/jellyfin
        // ELSE    $HOME/.config/jellyfin
        var configDir = options.ConfigDir;
        if (string.IsNullOrEmpty(configDir))
        {
            configDir = Environment.GetEnvironmentVariable("JELLYFIN_CONFIG_DIR");

            if (string.IsNullOrEmpty(configDir))
            {
                if (options.DataDir is not null
                    || Directory.Exists(Path.Combine(dataDir, "config"))
                    || OperatingSystem.IsWindows())
                {
                    // Hang config folder off already set dataDir
                    configDir = Path.Combine(dataDir, "config");
                }
                else
                {
                    // $XDG_CONFIG_HOME defines the base directory relative to which
                    // user specific configuration files should be stored.
                    configDir = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

                    // If $XDG_CONFIG_HOME is either not set or empty,
                    // a default equal to $HOME /.config should be used.
                    if (string.IsNullOrEmpty(configDir))
                    {
                        configDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            ".config");
                    }

                    configDir = Path.Combine(configDir, "jellyfin");
                }
            }
        }

        // cacheDir
        // IF      --cachedir
        // ELSE IF $JELLYFIN_CACHE_DIR
        // ELSE IF windows, use <datadir>/cache
        // ELSE IF XDG_CACHE_HOME, use $XDG_CACHE_HOME/jellyfin
        // ELSE    HOME/.cache/jellyfin
        var cacheDir = options.CacheDir;
        if (string.IsNullOrEmpty(cacheDir))
        {
            cacheDir = Environment.GetEnvironmentVariable("JELLYFIN_CACHE_DIR");

            if (string.IsNullOrEmpty(cacheDir))
            {
                if (OperatingSystem.IsWindows())
                {
                    // Hang cache folder off already set dataDir
                    cacheDir = Path.Combine(dataDir, "cache");
                }
                else
                {
                    // $XDG_CACHE_HOME defines the base directory relative to which
                    // user specific non-essential data files should be stored.
                    cacheDir = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");

                    // If $XDG_CACHE_HOME is either not set or empty,
                    // a default equal to $HOME/.cache should be used.
                    if (string.IsNullOrEmpty(cacheDir))
                    {
                        cacheDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            ".cache");
                    }

                    cacheDir = Path.Combine(cacheDir, "jellyfin");
                }
            }
        }

        // webDir
        // IF      --webdir
        // ELSE IF $JELLYFIN_WEB_DIR
        // ELSE    <bindir>/jellyfin-web
        var webDir = options.WebDir;
        if (string.IsNullOrEmpty(webDir))
        {
            webDir = Environment.GetEnvironmentVariable("JELLYFIN_WEB_DIR");

            if (string.IsNullOrEmpty(webDir))
            {
                // Use default location under ResourcesPath
                webDir = Path.Combine(AppContext.BaseDirectory, "jellyfin-web");
            }
        }

        // logDir
        // IF      --logdir
        // ELSE IF $JELLYFIN_LOG_DIR
        // ELSE IF --datadir, use <datadir>/log (assume portable run)
        // ELSE    <datadir>/log
        var logDir = options.LogDir;
        if (string.IsNullOrEmpty(logDir))
        {
            logDir = Environment.GetEnvironmentVariable("JELLYFIN_LOG_DIR");

            if (string.IsNullOrEmpty(logDir))
            {
                // Hang log folder off already set dataDir
                logDir = Path.Combine(dataDir, "log");
            }
        }

        // Normalize paths. Only possible with GetFullPath for now - https://github.com/dotnet/runtime/issues/2162
        dataDir = Path.GetFullPath(dataDir);
        logDir = Path.GetFullPath(logDir);
        configDir = Path.GetFullPath(configDir);
        cacheDir = Path.GetFullPath(cacheDir);
        webDir = Path.GetFullPath(webDir);

        // Ensure the main folders exist before we continue
        try
        {
            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(logDir);
            Directory.CreateDirectory(configDir);
            Directory.CreateDirectory(cacheDir);
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine("Error whilst attempting to create folder");
            Console.Error.WriteLine(ex.ToString());
            Environment.Exit(1);
        }

        return new ServerApplicationPaths(dataDir, logDir, configDir, cacheDir, webDir);
    }

    /// <summary>
    /// Gets the path for the unix socket Kestrel should bind to.
    /// </summary>
    /// <param name="startupConfig">The startup config.</param>
    /// <param name="appPaths">The application paths.</param>
    /// <returns>The path for Kestrel to bind to.</returns>
    public static string GetUnixSocketPath(IConfiguration startupConfig, IApplicationPaths appPaths)
    {
        var socketPath = startupConfig.GetUnixSocketPath();

        if (string.IsNullOrEmpty(socketPath))
        {
            var xdgRuntimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            var socketFile = "jellyfin.sock";
            if (xdgRuntimeDir is null)
            {
                // Fall back to config dir
                socketPath = Path.Join(appPaths.ConfigurationDirectoryPath, socketFile);
            }
            else
            {
                socketPath = Path.Join(xdgRuntimeDir, socketFile);
            }
        }

        return socketPath;
    }

    /// <summary>
    /// Sets the unix file permissions for Kestrel's socket file.
    /// </summary>
    /// <param name="startupConfig">The startup config.</param>
    /// <param name="socketPath">The socket path.</param>
    /// <param name="logger">The logger.</param>
    [UnsupportedOSPlatform("windows")]
    public static void SetUnixSocketPermissions(IConfiguration startupConfig, string socketPath, ILogger logger)
    {
        var socketPerms = startupConfig.GetUnixSocketPermissions();

        if (!string.IsNullOrEmpty(socketPerms))
        {
            File.SetUnixFileMode(socketPath, (UnixFileMode)Convert.ToInt32(socketPerms, 8));
            logger.LogInformation("Kestrel unix socket permissions set to {SocketPerms}", socketPerms);
        }
    }

    /// <summary>
    /// Initialize the logging configuration file using the bundled resource file as a default if it doesn't exist
    /// already.
    /// </summary>
    /// <param name="appPaths">The application paths.</param>
    /// <returns>A task representing the creation of the configuration file, or a completed task if the file already exists.</returns>
    public static async Task InitLoggingConfigFile(IApplicationPaths appPaths)
    {
        // Do nothing if the config file already exists
        string configPath = Path.Combine(appPaths.ConfigurationDirectoryPath, Program.LoggingConfigFileDefault);
        if (File.Exists(configPath))
        {
            return;
        }

        // Get a stream of the resource contents
        // NOTE: The .csproj name is used instead of the assembly name in the resource path
        const string ResourcePath = "Jellyfin.Server.Resources.Configuration.logging.json";
        Stream resource = typeof(Program).Assembly.GetManifestResourceStream(ResourcePath)
                          ?? throw new InvalidOperationException($"Invalid resource path: '{ResourcePath}'");
        await using (resource.ConfigureAwait(false))
        {
            Stream dst = new FileStream(configPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);
            await using (dst.ConfigureAwait(false))
            {
                // Copy the resource contents to the expected file path for the config file
                await resource.CopyToAsync(dst).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Initialize Serilog using configuration and fall back to defaults on failure.
    /// </summary>
    /// <param name="configuration">The configuration object.</param>
    /// <param name="appPaths">The application paths.</param>
    public static void InitializeLoggingFramework(IConfiguration configuration, IApplicationPaths appPaths)
    {
        try
        {
            // Serilog.Log is used by SerilogLoggerFactory when no logger is specified
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .CreateLogger();
        }
        catch (Exception ex)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] [{ThreadId}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture)
                .WriteTo.Async(x => x.File(
                    Path.Combine(appPaths.LogDirectoryPath, "log_.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{ThreadId}] {SourceContext}: {Message}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture,
                    encoding: Encoding.UTF8))
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .CreateLogger();

            Log.Logger.Fatal(ex, "Failed to create/read logger configuration");
        }
    }

    /// <summary>
    /// Call static initialization methods for the application.
    /// </summary>
    public static void PerformStaticInitialization()
    {
        // Make sure we have all the code pages we can get
        // Ref: https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider.instance?view=netcore-3.0#remarks
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Increase the max http request limit
        // The default connection limit is 10 for ASP.NET hosted applications and 2 for all others.
        ServicePointManager.DefaultConnectionLimit = Math.Max(96, ServicePointManager.DefaultConnectionLimit);

        // Disable the "Expect: 100-Continue" header by default
        // http://stackoverflow.com/questions/566437/http-post-returns-the-error-417-expectation-failed-c
        ServicePointManager.Expect100Continue = false;

        Batteries_V2.Init();
    }
}
