using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using System.ComponentModel.Composition;
using System.Linq;

namespace MediaBrowser.Controller.Resolvers.Audio
{
    [Export(typeof(IBaseItemResolver))]
    public class MusicArtistResolver : BaseItemResolver<MusicArtist>
    {
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Third; } // we need to be ahead of the generic folder resolver but behind the movie one
        }

        protected override MusicArtist Resolve(ItemResolveArgs args)
        {
            if (!args.IsDirectory) return null;

            //Avoid mis-identifying top folders
            if (args.Parent == null) return null;
            if (args.Parent.IsRoot) return null;

            // If we contain an album assume we are an artist folder
            return args.FileSystemChildren.Any(EntityResolutionHelper.IsMusicAlbum) ? new MusicArtist() : null;
        }

    }
}
