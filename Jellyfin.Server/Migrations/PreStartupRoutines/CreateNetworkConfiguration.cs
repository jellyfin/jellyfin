using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Emby.Server.Implementations;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.PreStartupRoutines;

/// <inheritdoc />
public class CreateNetworkConfiguration : IMigrationRoutine
{
    private readonly ServerApplicationPaths _applicationPaths;
    private readonly ILogger<CreateNetworkConfiguration> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateNetworkConfiguration"/> class.
    /// </summary>
    /// <param name="applicationPaths">An instance of <see cref="ServerApplicationPaths"/>.</param>
    /// <param name="loggerFactory">An instance of the <see cref="ILoggerFactory"/> interface.</param>
    public CreateNetworkConfiguration(ServerApplicationPaths applicationPaths, ILoggerFactory loggerFactory)
    {
        _applicationPaths = applicationPaths;
        _logger = loggerFactory.CreateLogger<CreateNetworkConfiguration>();
    }

    /// <inheritdoc />
    public Guid Id => Guid.Parse("9B354818-94D5-4B68-AC49-E35CB85F9D84");

    /// <inheritdoc />
    public string Name => nameof(CreateNetworkConfiguration);

    /// <inheritdoc />
    public bool PerformOnNewInstall => false;

    /// <inheritdoc />
    public void Perform()
    {
        string path = Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "network.xml");
        if (File.Exists(path))
        {
            _logger.LogDebug("Network configuration file already exists, skipping");
            return;
        }

        var serverConfigSerializer = new XmlSerializer(typeof(OldNetworkConfiguration), new XmlRootAttribute("ServerConfiguration"));
        using var xmlReader = XmlReader.Create(_applicationPaths.SystemConfigurationFilePath);
        var networkSettings = serverConfigSerializer.Deserialize(xmlReader);

        var networkConfigSerializer = new XmlSerializer(typeof(OldNetworkConfiguration), new XmlRootAttribute("NetworkConfiguration"));
        var xmlWriterSettings = new XmlWriterSettings { Indent = true };
        using var xmlWriter = XmlWriter.Create(path, xmlWriterSettings);
        networkConfigSerializer.Serialize(xmlWriter, networkSettings);
    }

#pragma warning disable
    public sealed class OldNetworkConfiguration
    {
        public const int DefaultHttpPort = 8096;

        public const int DefaultHttpsPort = 8920;

        private string _baseUrl = string.Empty;

        public bool RequireHttps { get; set; }

        public string CertificatePath { get; set; } = string.Empty;

        public string CertificatePassword { get; set; } = string.Empty;

        public string BaseUrl
        {
            get => _baseUrl;
            set
            {
                // Normalize the start of the string
                if (string.IsNullOrWhiteSpace(value))
                {
                    // If baseUrl is empty, set an empty prefix string
                    _baseUrl = string.Empty;
                    return;
                }

                if (value[0] != '/')
                {
                    // If baseUrl was not configured with a leading slash, append one for consistency
                    value = "/" + value;
                }

                // Normalize the end of the string
                if (value[^1] == '/')
                {
                    // If baseUrl was configured with a trailing slash, remove it for consistency
                    value = value.Remove(value.Length - 1);
                }

                _baseUrl = value;
            }
        }

        public int PublicHttpsPort { get; set; } = DefaultHttpsPort;

        public int HttpServerPortNumber { get; set; } = DefaultHttpPort;

        public int HttpsPortNumber { get; set; } = DefaultHttpsPort;

        public bool EnableHttps { get; set; }

        public int PublicPort { get; set; } = DefaultHttpPort;

        public bool EnableIPV6 { get; set; }

        public bool EnableIPV4 { get; set; } = true;

        public bool IgnoreVirtualInterfaces { get; set; } = true;

        public string[] VirtualInterfaceNames { get; set; } = new string[] { "veth" };

        public string[] PublishedServerUriBySubnet { get; set; } = Array.Empty<string>();

        public string[] RemoteIPFilter { get; set; } = Array.Empty<string>();

        public bool IsRemoteIPFilterBlacklist { get; set; }

        public bool EnableUPnP { get; set; }

        public bool EnableRemoteAccess { get; set; } = true;

        public string[] LocalNetworkSubnets { get; set; } = Array.Empty<string>();

        public string[] LocalNetworkAddresses { get; set; } = Array.Empty<string>();

        public string[] KnownProxies { get; set; } = Array.Empty<string>();
    }
}
