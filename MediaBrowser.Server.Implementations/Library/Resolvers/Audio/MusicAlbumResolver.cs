using System.Collections.Generic;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class MusicAlbumResolver
    /// </summary>
    public class MusicAlbumResolver : ItemResolver<MusicAlbum>
    {
        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Third; } // we need to be ahead of the generic folder resolver but behind the movie one
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>MusicAlbum.</returns>
        protected override MusicAlbum Resolve(ItemResolveArgs args)
        {
            if (!args.IsDirectory) return null;

            //Avoid mis-identifying top folders
            if (args.Parent == null) return null;
            if (args.Parent.IsRoot) return null;

            return IsMusicAlbum(args) ? new MusicAlbum() : null;
        }


        /// <summary>
        /// Determine if the supplied file data points to a music album
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns><c>true</c> if [is music album] [the specified data]; otherwise, <c>false</c>.</returns>
        public static bool IsMusicAlbum(WIN32_FIND_DATA data)
        {
            return ContainsMusic(FileSystem.GetFiles(data.Path));
        }

        /// <summary>
        /// Determine if the supplied reslove args should be considered a music album
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if [is music album] [the specified args]; otherwise, <c>false</c>.</returns>
        public static bool IsMusicAlbum(ItemResolveArgs args)
        {
            // Args points to an album if parent is an Artist folder or it directly contains music
            if (args.IsDirectory)
            {
                //if (args.Parent is MusicArtist) return true;  //saves us from testing children twice
                if (ContainsMusic(args.FileSystemChildren)) return true;
            }


            return false;
        }

        /// <summary>
        /// Determine if the supplied list contains what we should consider music
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns><c>true</c> if the specified list contains music; otherwise, <c>false</c>.</returns>
        public static bool ContainsMusic(IEnumerable<WIN32_FIND_DATA> list)
        {
            // If list contains at least 2 audio files or at least one and no video files consider it to contain music
            var foundAudio = 0;
            var foundVideo = 0;
            foreach (var file in list)
            {
                if (AudioResolver.IsAudioFile(file)) foundAudio++;
                if (foundAudio >= 2)
                {
                    return true;
                }
                if (EntityResolutionHelper.IsVideoFile(file.Path)) foundVideo++;
            }

            //  or a single audio file and no video files
            if (foundAudio > 0 && foundVideo == 0) return true;
            return false;
        }
    }
}
