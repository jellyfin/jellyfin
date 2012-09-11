using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System.ComponentModel.Composition;
using System.IO;

namespace MediaBrowser.Controller.Resolvers
{
    [Export(typeof(IBaseItemResolver))]
    public class AudioResolver : BaseItemResolver<Audio>
    {
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Last; }
        }
        
        protected override Audio Resolve(ItemResolveEventArgs args)
        {
            // Return audio if the path is a file and has a matching extension

            if (!args.IsDirectory)
            {
                if (IsAudioFile(args.Path))
                {
                    return new Audio();
                }
            }

            return null;
        }

        private static bool IsAudioFile(string path)
        {
            string extension = Path.GetExtension(path).ToLower();

            switch (extension)
            {
                case ".mp3":
                case ".wma":
                case ".aac":
                case ".acc":
                case ".flac":
                case ".m4a":
                case ".m4b":
                case ".wav":
                case ".ape":
                    return true;

                default:
                    return false;
            }

        }
    }
}
