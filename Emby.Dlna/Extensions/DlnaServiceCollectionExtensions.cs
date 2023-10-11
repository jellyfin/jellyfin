using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using Emby.Dlna.ConnectionManager;
using Emby.Dlna.ContentDirectory;
using Emby.Dlna.MediaReceiverRegistrar;
using Emby.Dlna.Ssdp;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rssdp.Infrastructure;

namespace Emby.Dlna.Extensions;

/// <summary>
/// Extension methods for adding DLNA services.
/// </summary>
public static class DlnaServiceCollectionExtensions
{
    /// <summary>
    /// Adds DLNA services to the provided <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="applicationHost">The <see cref="IServerApplicationHost"/>.</param>
    public static void AddDlnaServices(
        this IServiceCollection services,
        IServerApplicationHost applicationHost)
    {
        services.AddHttpClient(NamedClient.Dlna, c =>
            {
                c.DefaultRequestHeaders.UserAgent.ParseAdd(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}/{1} UPnP/1.0 {2}/{3}",
                        Environment.OSVersion.Platform,
                        Environment.OSVersion,
                        applicationHost.Name,
                        applicationHost.ApplicationVersionString));

                c.DefaultRequestHeaders.Add("CPFN.UPNP.ORG", applicationHost.FriendlyName); // Required for UPnP DeviceArchitecture v2.0
                c.DefaultRequestHeaders.Add("FriendlyName.DLNA.ORG", applicationHost.FriendlyName); // REVIEW: where does this come from?
            })
            .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                RequestHeaderEncodingSelector = (_, _) => Encoding.UTF8
            });

        services.AddSingleton<IDlnaManager, DlnaManager>();
        services.AddSingleton<IDeviceDiscovery, DeviceDiscovery>();
        services.AddSingleton<IContentDirectory, ContentDirectoryService>();
        services.AddSingleton<IConnectionManager, ConnectionManagerService>();
        services.AddSingleton<IMediaReceiverRegistrar, MediaReceiverRegistrarService>();

        services.AddSingleton<ISsdpCommunicationsServer>(provider => new SsdpCommunicationsServer(
            provider.GetRequiredService<ISocketFactory>(),
            provider.GetRequiredService<INetworkManager>(),
            provider.GetRequiredService<ILogger<SsdpCommunicationsServer>>())
        {
            IsShared = true
        });
    }
}
