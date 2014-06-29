using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;

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
            if (args.Parent is MusicAlbum) return null;

            // Optimization
            if (args.Parent is BoxSet || args.Parent is Series || args.Parent is Season)
            {
                return null;
            }

            var collectionType = args.GetCollectionType();

            // If there's a collection type and it's not music, don't allow it.
            if (!string.IsNullOrEmpty(collectionType) &&
                !string.Equals(collectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return IsMusicAlbum(args) ? new MusicAlbum() : null;
        }


        /// <summary>
        /// Determine if the supplied file data points to a music album
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns><c>true</c> if [is music album] [the specified data]; otherwise, <c>false</c>.</returns>
        public static bool IsMusicAlbum(string path, IDirectoryService directoryService)
        {
            return ContainsMusic(directoryService.GetFileSystemEntries(path), true, directoryService);
        }

        /// <summary>
        /// Determine if the supplied resolve args should be considered a music album
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if [is music album] [the specified args]; otherwise, <c>false</c>.</returns>
        public static bool IsMusicAlbum(ItemResolveArgs args)
        {
            // Args points to an album if parent is an Artist folder or it directly contains music
            if (args.IsDirectory)
            {
                //if (args.Parent is MusicArtist) return true;  //saves us from testing children twice
                if (ContainsMusic(args.FileSystemChildren, true, args.DirectoryService)) return true;
            }

            return false;
        }

        /// <summary>
        /// Determine if the supplied list contains what we should consider music
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="allowSubfolders">if set to <c>true</c> [allow subfolders].</param>
        /// <param name="directoryService">The directory service.</param>
        /// <returns><c>true</c> if the specified list contains music; otherwise, <c>false</c>.</returns>
        private static bool ContainsMusic(IEnumerable<FileSystemInfo> list, bool allowSubfolders, IDirectoryService directoryService)
        {
            // If list contains at least 2 audio files or at least one and no video files consider it to contain music
            var foundAudio = 0;

            foreach (var fileSystemInfo in list)
            {
                if ((fileSystemInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (allowSubfolders && IsAlbumSubfolder(fileSystemInfo, directoryService))
                    {
                        return true;
                    }
                    if (!IsAdditionalSubfolderAllowed(fileSystemInfo))
                    {
                        return false;
                    }
                }

                var fullName = fileSystemInfo.FullName;

                if (EntityResolutionHelper.IsAudioFile(fullName))
                {
                    // Don't resolve these into audio files
                    if (string.Equals(Path.GetFileNameWithoutExtension(fullName), BaseItem.ThemeSongFilename) && EntityResolutionHelper.IsAudioFile(fullName))
                    {
                        continue;
                    }

                    foundAudio++;
                }
                if (foundAudio >= 2)
                {
                    return true;
                }

                if (EntityResolutionHelper.IsVideoFile(fullName)) return false;
                if (EntityResolutionHelper.IsVideoPlaceHolder(fullName)) return false;
            }

            //  or a single audio file and no video files
            return foundAudio > 0;
        }

        private static bool IsAlbumSubfolder(FileSystemInfo directory, IDirectoryService directoryService)
        {
            var path = directory.FullName;

            if (IsMultiDiscFolder(path))
            {
                return ContainsMusic(directoryService.GetFileSystemEntries(path), false, directoryService);
            }

            return false;
        }

        private static bool IsMultiDiscFolder(string path)
        {
            return EntityResolutionHelper.IsMultiPartFolder(path);
        }

        private static bool IsAdditionalSubfolderAllowed(FileSystemInfo directory)
        {
            // TOOD: allow some metadata folders like extrafanart, extrathumbs
            return false;
        }
    }
}
