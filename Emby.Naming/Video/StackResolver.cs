using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.AudioBook;
using Emby.Naming.Common;
using MediaBrowser.Model.IO;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Resolve <see cref="FileStack"/> from list of paths.
    /// </summary>
    public class StackResolver
    {
        private readonly NamingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="StackResolver"/> class.
        /// </summary>
        /// <param name="options"><see cref="NamingOptions"/> object containing VideoFileStackingRegexes and passes options to <see cref="VideoResolver"/>.</param>
        public StackResolver(NamingOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Resolves only directories from paths.
        /// </summary>
        /// <param name="files">List of paths.</param>
        /// <returns>Enumerable <see cref="FileStack"/> of directories.</returns>
        public IEnumerable<FileStack> ResolveDirectories(IEnumerable<string> files)
        {
            return Resolve(files.Select(i => new FileSystemMetadata { FullName = i, IsDirectory = true }));
        }

        /// <summary>
        /// Resolves only files from paths.
        /// </summary>
        /// <param name="files">List of paths.</param>
        /// <returns>Enumerable <see cref="FileStack"/> of files.</returns>
        public IEnumerable<FileStack> ResolveFiles(IEnumerable<string> files)
        {
            return Resolve(files.Select(i => new FileSystemMetadata { FullName = i, IsDirectory = false }));
        }

        /// <summary>
        /// Resolves audiobooks from paths.
        /// </summary>
        /// <param name="files">List of paths.</param>
        /// <returns>Enumerable <see cref="FileStack"/> of directories.</returns>
        public IEnumerable<FileStack> ResolveAudioBooks(IEnumerable<AudioBookFileInfo> files)
        {
            var groupedDirectoryFiles = files.GroupBy(file => Path.GetDirectoryName(file.Path));

            foreach (var directory in groupedDirectoryFiles)
            {
                if (string.IsNullOrEmpty(directory.Key))
                {
                    foreach (var file in directory)
                    {
                        var stack = new FileStack { Name = Path.GetFileNameWithoutExtension(file.Path), IsDirectoryStack = false };
                        stack.Files.Add(file.Path);
                        yield return stack;
                    }
                }
                else
                {
                    var stack = new FileStack { Name = Path.GetFileName(directory.Key), IsDirectoryStack = false };
                    foreach (var file in directory)
                    {
                        stack.Files.Add(file.Path);
                    }

                    yield return stack;
                }
            }
        }

        /// <summary>
        /// Resolves videos from paths.
        /// </summary>
        /// <param name="files">List of paths.</param>
        /// <returns>Enumerable <see cref="FileStack"/> of videos.</returns>
        public IEnumerable<FileStack> Resolve(IEnumerable<FileSystemMetadata> files)
        {
            var resolver = new VideoResolver(_options);

            var list = files
                .Where(i => i.IsDirectory || resolver.IsVideoFile(i.FullName) || resolver.IsStubFile(i.FullName))
                .OrderBy(i => i.FullName)
                .ToList();

            var expressions = _options.VideoFileStackingRegexes;

            for (var i = 0; i < list.Count; i++)
            {
                var offset = 0;

                var file1 = list[i];

                var expressionIndex = 0;
                while (expressionIndex < expressions.Length)
                {
                    var exp = expressions[expressionIndex];
                    var stack = new FileStack();

                    // (Title)(Volume)(Ignore)(Extension)
                    var match1 = FindMatch(file1, exp, offset);

                    if (match1.Success)
                    {
                        var title1 = match1.Groups["title"].Value;
                        var volume1 = match1.Groups["volume"].Value;
                        var ignore1 = match1.Groups["ignore"].Value;
                        var extension1 = match1.Groups["extension"].Value;

                        var j = i + 1;
                        while (j < list.Count)
                        {
                            var file2 = list[j];

                            if (file1.IsDirectory != file2.IsDirectory)
                            {
                                j++;
                                continue;
                            }

                            // (Title)(Volume)(Ignore)(Extension)
                            var match2 = FindMatch(file2, exp, offset);

                            if (match2.Success)
                            {
                                var title2 = match2.Groups[1].Value;
                                var volume2 = match2.Groups[2].Value;
                                var ignore2 = match2.Groups[3].Value;
                                var extension2 = match2.Groups[4].Value;

                                if (string.Equals(title1, title2, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!string.Equals(volume1, volume2, StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (string.Equals(ignore1, ignore2, StringComparison.OrdinalIgnoreCase)
                                            && string.Equals(extension1, extension2, StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (stack.Files.Count == 0)
                                            {
                                                stack.Name = title1 + ignore1;
                                                stack.IsDirectoryStack = file1.IsDirectory;
                                                stack.Files.Add(file1.FullName);
                                            }

                                            stack.Files.Add(file2.FullName);
                                        }
                                        else
                                        {
                                            // Sequel
                                            offset = 0;
                                            expressionIndex++;
                                            break;
                                        }
                                    }
                                    else if (!string.Equals(ignore1, ignore2, StringComparison.OrdinalIgnoreCase))
                                    {
                                        // False positive, try again with offset
                                        offset = match1.Groups[3].Index;
                                        break;
                                    }
                                    else
                                    {
                                        // Extension mismatch
                                        offset = 0;
                                        expressionIndex++;
                                        break;
                                    }
                                }
                                else
                                {
                                    // Title mismatch
                                    offset = 0;
                                    expressionIndex++;
                                    break;
                                }
                            }
                            else
                            {
                                // No match 2, next expression
                                offset = 0;
                                expressionIndex++;
                                break;
                            }

                            j++;
                        }

                        if (j == list.Count)
                        {
                            expressionIndex = expressions.Length;
                        }
                    }
                    else
                    {
                        // No match 1
                        offset = 0;
                        expressionIndex++;
                    }

                    if (stack.Files.Count > 1)
                    {
                        yield return stack;
                        i += stack.Files.Count - 1;
                        break;
                    }
                }
            }
        }

        private static string GetRegexInput(FileSystemMetadata file)
        {
            // For directories, dummy up an extension otherwise the expressions will fail
            var input = !file.IsDirectory
                ? file.FullName
                : file.FullName + ".mkv";

            return Path.GetFileName(input);
        }

        private static Match FindMatch(FileSystemMetadata input, Regex regex, int offset)
        {
            var regexInput = GetRegexInput(input);

            if (offset < 0 || offset >= regexInput.Length)
            {
                return Match.Empty;
            }

            return regex.Match(regexInput, offset);
        }
    }
}
