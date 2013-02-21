using MediaBrowser.Controller.Library;
using System.ComponentModel.Composition;

namespace MediaBrowser.Controller.Resolvers.Audio
{
    [Export(typeof(IBaseItemResolver))]
    public class AudioResolver : BaseItemResolver<Entities.Audio.Audio>
    {
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Last; }
        }
        
        protected override Entities.Audio.Audio Resolve(ItemResolveArgs args)
        {
            // Return audio if the path is a file and has a matching extension

            if (!args.IsDirectory)
            {
                if (EntityResolutionHelper.IsAudioFile(args))
                {
                    return new Entities.Audio.Audio();
                }
            }

            return null;
        }
    }
}
