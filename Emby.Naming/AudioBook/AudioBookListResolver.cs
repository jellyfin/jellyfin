using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Model.IO;

namespace Emby.Naming.AudioBook
{
    public class AudioBookListResolver
    {
        private readonly NamingOptions _options;

        public AudioBookListResolver(NamingOptions options)
        {
            _options = options;
        }

        public IEnumerable<AudioBookInfo> Resolve(List<FileSystemMetadata> files)
        {
            var audioBookResolver = new AudioBookResolver(_options);

            var audiobookFileInfos = files
                .Select(i => audioBookResolver.Resolve(i.FullName, i.IsDirectory))
                .Where(i => i != null)
                .ToList();

            // Filter out all extras, otherwise they could cause stacks to not be resolved
            // See the unit test TestStackedWithTrailer
            var metadata = audiobookFileInfos
                .Select(i => new FileSystemMetadata
                {
                    FullName = i.Path,
                    IsDirectory = i.IsDirectory
                });

            var stackResult = new StackResolver(_options)
                .ResolveAudioBooks(metadata);

            var list = new List<AudioBookInfo>();

            foreach (var stack in stackResult.Stacks)
            {
                var stackFiles = stack.Files.Select(i => audioBookResolver.Resolve(i, stack.IsDirectoryStack)).ToList();
                stackFiles.Sort();
                var info = new AudioBookInfo
                {
                    Files = stackFiles,
                    Name = stack.Name
                };
                list.Add(info);
            }

            // Whatever files are left, just add them
            /*list.AddRange(remainingFiles.Select(i => new AudioBookInfo
            {
                Files = new List<AudioBookFileInfo> { i },
                Name = i.,
                Year = i.Year
            }));*/

            var orderedList = list.OrderBy(i => i.Name);

            return orderedList;
        }
    }
}
