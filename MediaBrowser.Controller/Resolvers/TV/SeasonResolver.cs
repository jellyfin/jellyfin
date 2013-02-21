using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using System.ComponentModel.Composition;
using System.IO;

namespace MediaBrowser.Controller.Resolvers.TV
{
    [Export(typeof(IBaseItemResolver))]
    public class SeasonResolver : BaseFolderResolver<Season>
    {
        protected override Season Resolve(ItemResolveEventArgs args)
        {
            if (args.Parent is Series && args.IsDirectory)
            {
                var season = new Season { };

                season.IndexNumber = TVUtils.GetSeasonNumberFromPath(args.Path);

                return season;
            }

            return null;
        }
    }
}
