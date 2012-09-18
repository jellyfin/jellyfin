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
            if (args.Parent is Series && args.IsDirectory && !args.IsMetadataFolder)
            {
                var season = new Season { };

                season.IndexNumber = TVUtils.GetSeasonNumberFromPath(args.Path);

                // Gather these now so that the episode provider classes can utilize them instead of having to make their own file system calls
                season.MetadataFiles = args.ContainsFolder("metadata") ? Directory.GetFiles(Path.Combine(args.Path, "metadata"), "*", SearchOption.TopDirectoryOnly) : new string[] { };

                return season;
            }

            return null;
        }
    }
}
