#pragma warning disable CS1591

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

        public IEnumerable<AudioBookInfo> Resolve(IEnumerable<FileSystemMetadata> files)
        {
            var audioBookResolver = new AudioBookResolver(_options);

            // File with empty fullname will be sorted out here
            var audiobookFileInfos = files
                .Select(i => audioBookResolver.Resolve(i.FullName))
                .OfType<AudioBookFileInfo>()
                .ToList();

            var stackResult = new StackResolver(_options)
                .ResolveAudioBooks(audiobookFileInfos);

            foreach (var stack in stackResult)
            {
                var stackFiles = stack.Files
                    .Select(i => audioBookResolver.Resolve(i))
                    .OfType<AudioBookFileInfo>()
                    .ToList();

                stackFiles.Sort();

                // stack.Name can be empty when we have file without folder, but always have some files
                var name = string.IsNullOrEmpty(stack.Name) ? stack.Files[0] : stack.Name;
                var info = new AudioBookInfo(name) { Files = stackFiles };

                yield return info;
            }
        }
    }
}
