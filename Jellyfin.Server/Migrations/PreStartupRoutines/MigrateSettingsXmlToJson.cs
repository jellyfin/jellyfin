using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Server.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Branding;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.PreStartupRoutines;

/// <summary>
/// Migrates the <see cref="BrandingOptions"/> XML file to JSON for use with the
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> system.
/// </summary>
[JellyfinMigration("2025-05-02T00:03:00", nameof(MigrateSettingsXmlToJson), Stage = Stages.JellyfinMigrationStageTypes.PreInitialisation)]
internal class MigrateSettingsXmlToJson : IAsyncMigrationRoutine
{
    private readonly ServerApplicationPaths _applicationPaths;
    private readonly ILogger<MigrateSettingsXmlToJson> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateSettingsXmlToJson"/> class.
    /// </summary>
    /// <param name="applicationPaths">An instance of <see cref="ServerApplicationPaths"/>.</param>
    /// <param name="loggerFactory">An instance of the <see cref="ILoggerFactory"/> interface.</param>
    public MigrateSettingsXmlToJson(ServerApplicationPaths applicationPaths, ILoggerFactory loggerFactory)
    {
        _applicationPaths = applicationPaths;
        _logger = loggerFactory.CreateLogger<MigrateSettingsXmlToJson>();
    }

    public static void Migrate<T>(string xmlSrcPath, string jsonDstPath, string sectionKey, ILogger logger)
        where T : class, new()
    {
        if (File.Exists(jsonDstPath))
        {
            return;
        }

        var xmlSerializer = new MyXmlSerializer();
        T config;

        if (File.Exists(xmlSrcPath))
        {
            try
            {
                config = (T?)xmlSerializer.DeserializeFromFile(typeof(T), xmlSrcPath) ?? new T();
                logger.LogInformation(
                    "Migrating {ConfigType} from XML ({XmlPath}) to JSON ({JsonPath})",
                    typeof(T).Name,
                    xmlSrcPath,
                    jsonDstPath);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to read {ConfigType} from {XmlPath}; writing defaults to {JsonPath}",
                    typeof(T).Name,
                    xmlSrcPath,
                    jsonDstPath);
                config = new T();
            }
        }
        else
        {
            logger.LogInformation(
                "No existing XML for {ConfigType}; writing defaults to {JsonPath}",
                typeof(T).Name,
                jsonDstPath);
            config = new T();
        }

        var root = new JsonObject
        {
            [sectionKey] = JsonSerializer.SerializeToNode(config)
        };

        Directory.CreateDirectory(Path.GetDirectoryName(jsonDstPath)!);
        File.WriteAllText(jsonDstPath, root.ToJsonString(_jsonOptions));
    }

    /// <inheritdoc/>
    public Task PerformAsync(CancellationToken cancellationToken)
    {
        Migrate<BrandingOptions>(
            Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "branding.xml"),
            Path.Combine(_applicationPaths.ConfigurationDirectoryPath, JellyfinConfigurationConstants.BrandingJsonFile),
            JellyfinConfigurationConstants.BrandingOptionsKey,
            _logger);

        Migrate<EncodingOptions>(
            Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "encoding.xml"),
            Path.Combine(_applicationPaths.ConfigurationDirectoryPath, JellyfinConfigurationConstants.EncodingJsonFile),
            JellyfinConfigurationConstants.EncodingOptionsKey,
            _logger);

        Migrate<NetworkConfiguration>(
            Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "network.xml"),
            Path.Combine(_applicationPaths.ConfigurationDirectoryPath, JellyfinConfigurationConstants.NetworkJsonFile),
            JellyfinConfigurationConstants.NetworkConfigurationKey,
            _logger);

        Migrate<ServerConfiguration>(
            _applicationPaths.SystemConfigurationFilePath,
            Path.Combine(_applicationPaths.ConfigurationDirectoryPath, JellyfinConfigurationConstants.SystemJsonFile),
            JellyfinConfigurationConstants.ServerConfigurationKey,
            _logger);
        return Task.CompletedTask;
    }
}
