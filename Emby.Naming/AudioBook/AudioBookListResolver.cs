#pragma warning disable CS1591

using System.Collections.Generic;
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

        public IEnumerable<AudioBookInfo> Resolve(IEnumerable<FileSystemMetadata> files)
        {
            var audioBookResolver = new AudioBookResolver(_options);

            var audiobookFileInfos = files
                .Select(i => audioBookResolver.Resolve(i.FullName))
                .OfType<AudioBookFileInfo>()
                .ToList();

            // Filter out all extras, otherwise they could cause stacks to not be resolved
            // See the unit test TestStackedWithTrailer
            var metadata = audiobookFileInfos
                .Select(i => new FileSystemMetadata { FullName = i.Path, IsDirectory = false });

            var stackResult = new StackResolver(_options)
                .ResolveAudioBooks(audiobookFileInfos);

            foreach (var stack in stackResult)
            {
                var stackFiles = stack.Files.Select(i => audioBookResolver.Resolve(i)).OfType<AudioBookFileInfo>().ToList();
                stackFiles.Sort();
                // TODO nullable discover if name can be empty
                var info = new AudioBookInfo(stack.Name ?? string.Empty) { Files = stackFiles };

                yield return info;
            }
        }
    }
}
