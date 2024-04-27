#nullable disable

#pragma warning disable CS1591

using System.IO;
using System.Text.RegularExpressions;
using Emby.Naming.Audio;
using Emby.Naming.Common;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities.AudioBooks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.AudioBooks
{
    /// <summary>
    /// Resolve an AudioBook instance from a file system path.
    /// Resolve returns a partially initialized AudioBook instance with name (title), year, and path.
    /// Rely on audio file metadata for majority of other information.
    /// NOTE: Should these files be moved to MediaBrowser.Controller.Resolvers.(AudioBooks?) directory?.
    /// </summary>
    public class AudioBookResolver : GenericFolderResolver<AudioBook>
    {
        private readonly ILogger<AudioBookResolver> _logger;
        private readonly NamingOptions _namingOptions;
        private readonly IDirectoryService _directoryService;

        public AudioBookResolver(ILogger<AudioBookResolver> logger, NamingOptions namingOptions, IDirectoryService directoryService)
        {
            _logger = logger;
            _namingOptions = namingOptions;
            _directoryService = directoryService;
        }

        // <summary>
        // Gets the priority.
        // </summary>
        // <value>The priority.</value>
        // public override ResolverPriority Priority => ResolverPriority.Fourth;

        /// <summary>
        /// Attempt to resolve a single audio book from args.Path.
        /// Expected cases:
        /// Path is an AudioBook directory: Resolve path as AudioBook and children as AudioBookFiles.
        /// Path is single-file AudioBook: Resolve path as AudioBook w/ AudioBookFile of path.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Entities.AudioBook.</returns>
        protected override AudioBook Resolve(ItemResolveArgs args)
        {
            // We only care about audiobooks
            // TODO: Change this to new CollectionType.audiobooks?
            if (args.GetCollectionType() != CollectionType.books)
            {
                return null;
            }

            // TODO: I don't know if there's a valid case where parent is null and CollectionType is books
            if (args.Parent != null && args.Parent.GetType() != typeof(MediaBrowser.Controller.Entities.Folder))
            {
                return null;
            }

            var audioBook = new AudioBook();

            // TODO: Get title from path...
            var bookFileName = Path.GetFileNameWithoutExtension(args.Path);
            var name = MatchFor(bookFileName, "name");
            if (name == null)
            {
                name = bookFileName;
            }

            var year = MatchFor(bookFileName, "year");

            // Process directory to resolve single audio book
            if (args.IsDirectory)
            {
                // Majority children must be audio files
                int audioFiles = 0;
                int nonAudioFiles = 0;
                // TODO: What's the fancy C# way to do this?
                for (var i = 0; i < args.FileSystemChildren.Length; i++)
                {
                    var child = args.FileSystemChildren[i];
                    if (AudioFileParser.IsAudioFile(child.FullName, _namingOptions))
                    {
                        audioFiles += 1;
                    }
                    else
                    {
                        nonAudioFiles += 1;
                    }
                }

                if (audioFiles == 0 || nonAudioFiles / audioFiles > 1)
                {
                    _logger.LogDebug("Less than half of the files in {0} were audio files, probably not an AudioBook directory", args.Path);
                    return null;
                }

                // TODO: Must not contain sub directories
                return audioBook;
            }

            // Resolve a single-file AudioBook
            if (AudioFileParser.IsAudioFile(args.Path, _namingOptions))
            {
                // Create AudioBookFile
                var audioBookFile = new AudioBookFile
                {
                    Path = args.Path
                };

                // Set as child of audioBook
                // TODO: Is making both calls redundant?
                audioBookFile.SetParent(audioBook);
                audioBook.AddChild(audioBookFile);

                var extension = Path.GetExtension(args.Path);

                audioBookFile.Container = extension.TrimStart('.');
                audioBookFile.Chapter = 0;
                // audioBookFile.path = args.path; ?

                return audioBook;
            }

            return null;
        }

        // TODO: Put this somewhere better where both AudioBook classes can access
        private string MatchFor(string name, string matchGroup)
        {
            foreach (var expression in _namingOptions.AudioBookNamesExpressions)
            {
                var match = Regex.Match(name, expression, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var value = match.Groups[matchGroup];
                    if (value.Success)
                    {
                        return value.Value;
                    }
                }
            }

            return null;
        }
    }
}
