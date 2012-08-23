using System;
using System.ComponentModel.Composition;
using System.IO;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Movies.Entities;

namespace MediaBrowser.Movies.Resolvers
{
    [Export(typeof(IBaseItemResolver))]
    public class BoxSetResolver : BaseFolderResolver<BoxSet>
    {
        protected override BoxSet Resolve(ItemResolveEventArgs args)
        {
            // It's a boxset if all of the following conditions are met:
            // It's under a 'Movies' VF
            // Is a Directory
            // Contains [boxset] in the path
            if ((args.VirtualFolderCollectionType ?? string.Empty).Equals("Movies", StringComparison.OrdinalIgnoreCase) && args.IsDirectory)
            {
                if (Path.GetFileName(args.Path).IndexOf("[boxset]", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return new BoxSet();
                }
            }

            return null;
        }
    }
}
