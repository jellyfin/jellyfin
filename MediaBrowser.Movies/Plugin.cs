using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Movies.Entities;
using MediaBrowser.Movies.Resolvers;

namespace MediaBrowser.Movies
{
    public class Plugin : BaseGenericPlugin<BasePluginConfiguration>
    {
        public override string Name
        {
            get { return "Movies"; }
        }

        public override void InitInServer()
        {
            Kernel.Instance.AddBaseItemType<BoxSet, BoxSetResolver>();
            Kernel.Instance.AddBaseItemType<Movie, MovieResolver>();
        }
    }
}
