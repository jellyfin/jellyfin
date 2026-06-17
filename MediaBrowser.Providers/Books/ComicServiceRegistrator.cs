using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Providers.Books.ComicBookInfo;
using MediaBrowser.Providers.Books.ComicInfo;
using Microsoft.Extensions.DependencyInjection;

namespace MediaBrowser.Providers.Books;

/// <inheritdoc />
public class ComicServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // register the generic local metadata provider for comic files
        serviceCollection.AddSingleton<ComicProvider>();

        // register the actual implementations of the local metadata provider for comic files
        serviceCollection.AddSingleton<IComicProvider, ComicBookInfoProvider>();
        serviceCollection.AddSingleton<IComicProvider, ExternalComicInfoProvider>();
        serviceCollection.AddSingleton<IComicProvider, InternalComicInfoProvider>();
    }
}
