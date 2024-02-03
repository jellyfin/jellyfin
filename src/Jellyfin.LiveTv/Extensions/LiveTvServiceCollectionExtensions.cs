using Jellyfin.LiveTv.Channels;
using Jellyfin.LiveTv.Guide;
using Jellyfin.LiveTv.TunerHosts;
using Jellyfin.LiveTv.TunerHosts.HdHomerun;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.LiveTv.Extensions;

/// <summary>
/// Live TV extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class LiveTvServiceCollectionExtensions
{
    /// <summary>
    /// Adds Live TV services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    public static void AddLiveTvServices(this IServiceCollection services)
    {
        services.AddSingleton<LiveTvDtoService>();
        services.AddSingleton<ILiveTvManager, LiveTvManager>();
        services.AddSingleton<IChannelManager, ChannelManager>();
        services.AddSingleton<IStreamHelper, StreamHelper>();
        services.AddSingleton<ITunerHostManager, TunerHostManager>();
        services.AddSingleton<IGuideManager, GuideManager>();

        services.AddSingleton<ITunerHost, HdHomerunHost>();
        services.AddSingleton<ITunerHost, M3UTunerHost>();
    }
}
