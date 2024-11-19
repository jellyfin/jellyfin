using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Emby.Server.Implementations;
using MediaBrowser.Providers.Plugins.MusicBrainz.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.PreStartupRoutines;

/// <inheritdoc />
public class MigrateMusicBrainzTimeout : IMigrationRoutine
{
    private readonly ServerApplicationPaths _applicationPaths;
    private readonly ILogger<MigrateMusicBrainzTimeout> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateMusicBrainzTimeout"/> class.
    /// </summary>
    /// <param name="applicationPaths">An instance of <see cref="ServerApplicationPaths"/>.</param>
    /// <param name="loggerFactory">An instance of the <see cref="ILoggerFactory"/> interface.</param>
    public MigrateMusicBrainzTimeout(ServerApplicationPaths applicationPaths, ILoggerFactory loggerFactory)
    {
        _applicationPaths = applicationPaths;
        _logger = loggerFactory.CreateLogger<MigrateMusicBrainzTimeout>();
    }

    /// <inheritdoc />
    public Guid Id => Guid.Parse("A6DCACF4-C057-4Ef9-80D3-61CEF9DDB4F0");

    /// <inheritdoc />
    public string Name => nameof(MigrateMusicBrainzTimeout);

    /// <inheritdoc />
    public bool PerformOnNewInstall => false;

    /// <inheritdoc />
    public void Perform()
    {
        string path = Path.Combine(_applicationPaths.PluginConfigurationsPath, "Jellyfin.Plugin.MusicBrainz.xml");
        if (!File.Exists(path))
        {
            _logger.LogDebug("No MusicBrainz plugin configuration file found, skipping");
            return;
        }

        var oldPluginConfiguration = ReadOld(path);

        if (oldPluginConfiguration is not null)
        {
            var newPluginConfiguration = new PluginConfiguration
            {
                Server = oldPluginConfiguration.Server,
                ReplaceArtistName = oldPluginConfiguration.ReplaceArtistName
            };
            var newRateLimit = oldPluginConfiguration.RateLimit / 1000.0;
            newPluginConfiguration.RateLimit = newRateLimit < 1.0 ? 1.0 : newRateLimit;
            WriteNew(path, newPluginConfiguration);
        }
    }

    private OldMusicBrainzConfiguration? ReadOld(string path)
    {
        using var xmlReader = XmlReader.Create(path);
        var serverConfigSerializer = new XmlSerializer(typeof(OldMusicBrainzConfiguration), new XmlRootAttribute("PluginConfiguration"));
        return serverConfigSerializer.Deserialize(xmlReader) as OldMusicBrainzConfiguration;
    }

    private void WriteNew(string path, PluginConfiguration newPluginConfiguration)
    {
        var pluginConfigurationSerializer = new XmlSerializer(typeof(PluginConfiguration), new XmlRootAttribute("PluginConfiguration"));
        var xmlWriterSettings = new XmlWriterSettings { Indent = true };
        using var xmlWriter = XmlWriter.Create(path, xmlWriterSettings);
        pluginConfigurationSerializer.Serialize(xmlWriter, newPluginConfiguration);
    }

#pragma warning disable
    public sealed class OldMusicBrainzConfiguration
    {
        private string _server = string.Empty;

        private long _rateLimit = 0L;

        public string Server
        {
            get => _server;
            set => _server = value.TrimEnd('/');
        }

        public long RateLimit
        {
            get => _rateLimit;
            set => _rateLimit = value;
        }

        public bool ReplaceArtistName { get; set; }
    }
}
