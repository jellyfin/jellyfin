using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Model.IO;

namespace Emby.Naming.AudioBook
{
    /// <summary>
    /// Class used to resolve Name, Year, alternative files and extras from stack of files.
    /// </summary>
    public class AudioBookListResolver
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBookListResolver"/> class.
        /// </summary>
        /// <param name="options">Naming options passed along to <see cref="AudioBookResolver"/> and <see cref="AudioBookNameParser"/>.</param>
        public AudioBookListResolver(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Resolves Name, Year and differentiate alternative files and extras from regular audiobook files.
        /// </summary>
        /// <param name="files">List of files related to audiobook.</param>
        /// <returns>Returns IEnumerable of <see cref="AudioBookInfo"/>.</returns>
        public IEnumerable<AudioBookInfo> Resolve(IEnumerable<FileSystemMetadata> files)
        {
            var audioBookResolver = new AudioBookResolver(_options);

            // File with empty fullname will be sorted out here.
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

                var nameParserResult = new AudioBookNameParser(_options).Parse(stack.Name);

                FindExtraAndAlternativeFiles(ref stackFiles, out var extras, out var alternativeVersions, nameParserResult);

                var info = new AudioBookInfo(
                    nameParserResult.Name,
                    nameParserResult.Year,
                    stackFiles,
                    extras,
                    alternativeVersions);

                yield return info;
            }
        }

        private void FindExtraAndAlternativeFiles(ref List<AudioBookFileInfo> stackFiles, out List<AudioBookFileInfo> extras, out List<AudioBookFileInfo> alternativeVersions, AudioBookNameParserResult nameParserResult)
        {
            extras = new List<AudioBookFileInfo>();
            alternativeVersions = new List<AudioBookFileInfo>();

            var haveChaptersOrPages = stackFiles.Any(x => x.ChapterNumber != null || x.PartNumber != null);
            var groupedBy = stackFiles.GroupBy(file => new { file.ChapterNumber, file.PartNumber });
            var nameWithReplacedDots = nameParserResult.Name.Replace(" ", ".");

            foreach (var group in groupedBy)
            {
                if (group.Key.ChapterNumber == null && group.Key.PartNumber == null)
                {
                    if (group.Count() > 1 || haveChaptersOrPages)
                    {
                        var ex = new List<AudioBookFileInfo>();
                        var alt = new List<AudioBookFileInfo>();

                        foreach (var audioFile in group)
                        {
                            var name = Path.GetFileNameWithoutExtension(audioFile.Path);
                            if (name.Equals("audiobook") ||
                                name.Contains(nameParserResult.Name, StringComparison.OrdinalIgnoreCase) ||
                                name.Contains(nameWithReplacedDots, StringComparison.OrdinalIgnoreCase))
                            {
                                alt.Add(audioFile);
                            }
                            else
                            {
                                ex.Add(audioFile);
                            }
                        }

                        if (ex.Count > 0)
                        {
                            var extra = ex
                                .OrderBy(x => x.Container)
                                .ThenBy(x => x.Path)
                                .ToList();

                            stackFiles = stackFiles.Except(extra).ToList();
                            extras.AddRange(extra);
                        }

                        if (alt.Count > 0)
                        {
                            var alternatives = alt
                                .OrderBy(x => x.Container)
                                .ThenBy(x => x.Path)
                                .ToList();

                            var main = FindMainAudioBookFile(alternatives, nameParserResult.Name);
                            alternatives.Remove(main);
                            stackFiles = stackFiles.Except(alternatives).ToList();
                            alternativeVersions.AddRange(alternatives);
                        }
                    }
                }
                else if (group.Count() > 1)
                {
                    var alternatives = group
                        .OrderBy(x => x.Container)
                        .ThenBy(x => x.Path)
                        .Skip(1)
                        .ToList();

                    stackFiles = stackFiles.Except(alternatives).ToList();
                    alternativeVersions.AddRange(alternatives);
                }
            }
        }

        private AudioBookFileInfo FindMainAudioBookFile(List<AudioBookFileInfo> files, string name)
        {
            var main = files.Find(x => Path.GetFileNameWithoutExtension(x.Path).Equals(name, StringComparison.OrdinalIgnoreCase));
            main ??= files.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.Path).Equals("audiobook", StringComparison.OrdinalIgnoreCase));
            main ??= files.OrderBy(x => x.Container)
                .ThenBy(x => x.Path)
                .First();

            return main;
        }
    }
}
