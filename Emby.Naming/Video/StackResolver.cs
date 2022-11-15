using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                        var stack = new FileStack(Path.GetFileNameWithoutExtension(file.Path), false, new[] { file.Path });
                        yield return stack;
                    }
                }
                else
                {
                    var stack = new FileStack(Path.GetFileName(directory.Key), false, directory.Select(f => f.Path).ToArray());
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
            var potentialFiles = files
                .Where(i => i.IsDirectory || VideoResolver.IsVideoFile(i.FullName, namingOptions) || VideoResolver.IsStubFile(i.FullName, namingOptions))
                .OrderBy(i => i.FullName);

            var potentialStacks = new Dictionary<string, StackMetadata>();
            foreach (var file in potentialFiles)
            {
                var name = file.Name;
                if (string.IsNullOrEmpty(name))
                {
                    name = Path.GetFileName(file.FullName);
                }

                for (var i = 0; i < namingOptions.VideoFileStackingRules.Length; i++)
                {
                    var rule = namingOptions.VideoFileStackingRules[i];
                    if (!rule.Match(name, out var stackParsingResult))
                    {
                        continue;
                    }

                    var stackName = stackParsingResult.Value.StackName;
                    var partNumber = stackParsingResult.Value.PartNumber;
                    var partType = stackParsingResult.Value.PartType;

                    if (!potentialStacks.TryGetValue(stackName, out var stackResult))
                    {
                        stackResult = new StackMetadata(file.IsDirectory, rule.IsNumerical, partType);
                        potentialStacks[stackName] = stackResult;
                    }

                    if (stackResult.Parts.Count > 0)
                    {
                        if (stackResult.IsDirectory != file.IsDirectory
                            || !string.Equals(partType, stackResult.PartType, StringComparison.OrdinalIgnoreCase)
                            || stackResult.ContainsPart(partNumber))
                        {
                            continue;
                        }

                        if (rule.IsNumerical != stackResult.IsNumerical)
                        {
                            break;
                        }
                    }

                    stackResult.Parts.Add(partNumber, file);
                    break;
                }
            }

            foreach (var (fileName, stack) in potentialStacks)
            {
                if (stack.Parts.Count < 2)
                {
                    continue;
                }

                yield return new FileStack(fileName, stack.IsDirectory, stack.Parts.Select(kv => kv.Value.FullName).ToArray());
            }
        }

        private class StackMetadata
        {
            public StackMetadata(bool isDirectory, bool isNumerical, string partType)
            {
                Parts = new Dictionary<string, FileSystemMetadata>(StringComparer.OrdinalIgnoreCase);
                IsDirectory = isDirectory;
                IsNumerical = isNumerical;
                PartType = partType;
            }

            public Dictionary<string, FileSystemMetadata> Parts { get; }

            public bool IsDirectory { get; }

            public bool IsNumerical { get; }

            public string PartType { get; }

            public bool ContainsPart(string partNumber) => Parts.ContainsKey(partNumber);
        }
    }
}
