using System;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.TV.Entities;

namespace MediaBrowser.TV.Resolvers
{
    class SeasonResolver : BaseFolderResolver<Season>
    {
        protected override Season Resolve(ItemResolveEventArgs args)
        {
            if (args.IsFolder && args.Parent is Series)
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
