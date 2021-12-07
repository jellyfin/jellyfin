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
    public static class StackResolver
    {
        /// <summary>
        /// Resolves only directories from paths.
        /// </summary>
        /// <param name="files">List of paths.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <returns>Enumerable <see cref="FileStack"/> of directories.</returns>
        public static IEnumerable<FileStack> ResolveDirectories(IEnumerable<string> files, NamingOptions namingOptions)
        {
            return Resolve(files.Select(i => new FileSystemMetadata { FullName = i, IsDirectory = true }), namingOptions);
        }

        /// <summary>
        /// Resolves only files from paths.
        /// </summary>
        /// <param name="files">List of paths.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <returns>Enumerable <see cref="FileStack"/> of files.</returns>
        public static IEnumerable<FileStack> ResolveFiles(IEnumerable<string> files, NamingOptions namingOptions)
        {
            return Resolve(files.Select(i => new FileSystemMetadata { FullName = i, IsDirectory = false }), namingOptions);
        }

        /// <summary>
        /// Resolves audiobooks from paths.
        /// </summary>
        /// <param name="files">List of paths.</param>
        /// <returns>Enumerable <see cref="FileStack"/> of directories.</returns>
        public static IEnumerable<FileStack> ResolveAudioBooks(IEnumerable<AudioBookFileInfo> files)
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
        /// <param name="namingOptions">The naming options.</param>
        /// <returns>Enumerable <see cref="FileStack"/> of videos.</returns>
        public static IEnumerable<FileStack> Resolve(IEnumerable<FileSystemMetadata> files, NamingOptions namingOptions)
        {
            var list = files
                .Where(i => i.IsDirectory || VideoResolver.IsVideoFile(i.FullName, namingOptions) || VideoResolver.IsStubFile(i.FullName, namingOptions))
                .OrderBy(i => i.FullName)
                .Select(f => (f.IsDirectory, FileName: GetFileNameWithExtension(f), f.FullName))
                .ToList();

            // TODO is there a "nicer" way?
            var cache = new Dictionary<(string, Regex, int), Match>();

            var expressions = namingOptions.VideoFileStackingRegexes;

            for (var i = 0; i < list.Count; i++)
            {
                var offset = 0;

                var file1 = list[i];

                var expressionIndex = 0;
                while (expressionIndex < expressions.Length)
                {
                    var exp = expressions[expressionIndex];
                    FileStack? stack = null;

                    // (Title)(Volume)(Ignore)(Extension)
                    var match1 = FindMatch(file1.FileName, exp, offset, cache);

                    if (match1.Success)
                    {
                        var title1 = match1.Groups[1].Value;
                        var volume1 = match1.Groups[2].Value;
                        var ignore1 = match1.Groups[3].Value;
                        var extension1 = match1.Groups[4].Value;

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
                            var match2 = FindMatch(file2.FileName, exp, offset, cache);

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
                                            stack ??= new FileStack();
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

                    if (stack?.Files.Count > 1)
                    {
                        yield return stack;
                        i += stack.Files.Count - 1;
                        break;
                    }
                }
            }
        }

        private static string GetFileNameWithExtension(FileSystemMetadata file)
        {
            // For directories, dummy up an extension otherwise the expressions will fail
            var input = file.FullName;
            if (file.IsDirectory)
            {
                input = Path.ChangeExtension(input, "mkv");
            }

            return Path.GetFileName(input);
        }

        private static Match FindMatch(string input, Regex regex, int offset, Dictionary<(string, Regex, int), Match> cache)
        {
            if (offset < 0 || offset >= input.Length)
            {
                return Match.Empty;
            }

            if (!cache.TryGetValue((input, regex, offset), out var result))
            {
                result = regex.Match(input, offset, input.Length - offset);
                cache.Add((input, regex, offset), result);
            }

            return result;
        }
    }
}
