#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Emby.Naming.Common;
using MediaBrowser.Model.IO;

namespace Emby.Naming.Video
{
    public class StackResolver
    {
        private readonly NamingOptions _options;

        public StackResolver(NamingOptions options)
        {
            _options = options;
        }

        public IEnumerable<FileStack> ResolveDirectories(IEnumerable<string> files)
        {
            return Resolve(files.Select(i => new FileSystemMetadata { FullName = i, IsDirectory = true }));
        }

        public IEnumerable<FileStack> ResolveFiles(IEnumerable<string> files)
        {
            return Resolve(files.Select(i => new FileSystemMetadata { FullName = i, IsDirectory = false }));
        }

        public IEnumerable<FileStack> ResolveAudioBooks(IEnumerable<FileSystemMetadata> files)
        {
            var groupedDirectoryFiles = files.GroupBy(file =>
                file.IsDirectory
                    ? file.FullName
                    : Path.GetDirectoryName(file.FullName));

            foreach (var directory in groupedDirectoryFiles)
            {
                var stack = new FileStack { Name = Path.GetFileName(directory.Key), IsDirectoryStack = false };
                foreach (var file in directory)
                {
                    if (file.IsDirectory)
                    {
                        continue;
                    }

                    stack.Files.Add(file.FullName);
                }

                yield return stack;
            }
        }

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
