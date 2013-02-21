using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using System.ComponentModel.Composition;

namespace MediaBrowser.Controller.Resolvers.Audio
{
    [Export(typeof(IBaseItemResolver))]
    public class MusicAlbumResolver : BaseItemResolver<MusicAlbum>
    {
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Third; } // we need to be ahead of the generic folder resolver but behind the movie one
        }
        
        protected override MusicAlbum Resolve(ItemResolveArgs args)
        {
            if (!args.IsDirectory) return null;

            //Avoid mis-identifying top folders
            if (args.Parent == null) return null;
            if (args.Parent.IsRoot) return null;

            return EntityResolutionHelper.IsMusicAlbum(args) ? new MusicAlbum() : null;
        }

    }
}
