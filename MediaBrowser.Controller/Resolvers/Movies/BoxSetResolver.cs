using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using System;
using System.ComponentModel.Composition;
using System.IO;

namespace MediaBrowser.Controller.Resolvers.Movies
{
    [Export(typeof(IBaseItemResolver))]
    public class BoxSetResolver : BaseFolderResolver<BoxSet>
    {
        protected override BoxSet Resolve(ItemResolveEventArgs args)
        {
            // It's a boxset if all of the following conditions are met:
            // Is a Directory
            // Contains [boxset] in the path
            if (args.IsDirectory)
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
