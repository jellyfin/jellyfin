using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities.TV;
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
                Season season = new Season();

                season.IndexNumber = TVUtils.GetSeasonNumberFromPath(args.Path);

                // Gather these now so that the episode provider classes can utilize them instead of having to make their own file system calls
                if (args.ContainsFolder("metadata"))
                {
                    season.MetadataFiles = Directory.GetFiles(Path.Combine(args.Path, "metadata"), "*", SearchOption.TopDirectoryOnly);
                }
                else
                {
                    season.MetadataFiles = new string[] { };
                }

                return season;
            }

            return null;
        }
    }
}
