using System.ComponentModel.Composition;
using System.IO;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Resolvers
{
    [Export(typeof(IBaseItemResolver))]
    public class SeasonResolver : BaseFolderResolver<Season>
    {
        protected override Season Resolve(ItemResolveEventArgs args)
        {
            if (args.Parent is Series && args.IsDirectory)
            {
                Season season = new Season();

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
