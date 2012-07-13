using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Movies.Entities;
using MediaBrowser.Movies.Resolvers;

namespace MediaBrowser.Movies
{
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        protected override void InitInternal()
        {
            Kernel.Instance.AddBaseItemType<BoxSet, BoxSetResolver>();
            Kernel.Instance.AddBaseItemType<Movie, MovieResolver>();
        }
    }
}
