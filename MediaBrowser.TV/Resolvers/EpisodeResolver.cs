using System.ComponentModel.Composition;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Resolvers
{
    [Export(typeof(IBaseItemResolver))]
    public class EpisodeResolver : BaseVideoResolver<Episode>
    {
        protected override Episode Resolve(ItemResolveEventArgs args)
        {
            if (args.Parent is Season || args.Parent is Series)
            {
                return base.Resolve(args);
            }

            return null;
        }
    }
}
