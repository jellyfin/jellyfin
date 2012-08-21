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
