using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Emby.Naming.Common;
using Emby.Naming.Video;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using static Emby.Naming.Video.ExtraRuleResolver;

namespace Emby.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Resolves a Path into a Video or Video subclass.
    /// </summary>
    internal class ExtraResolver : BaseVideoResolver<Video>
    {
        private readonly NamingOptions _namingOptions;
        private readonly IItemResolver[] _trailerResolvers;
        private readonly IItemResolver[] _videoResolvers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtraResolver"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="namingOptions">An instance of <see cref="NamingOptions"/>.</param>
        /// <param name="directoryService">The directory service.</param>
        public ExtraResolver(ILogger<ExtraResolver> logger, NamingOptions namingOptions, IDirectoryService directoryService)
            : base(logger, namingOptions, directoryService)
        {
            _namingOptions = namingOptions;
            _trailerResolvers = new IItemResolver[] { new GenericVideoResolver<Trailer>(logger, namingOptions, directoryService) };
            _videoResolvers = new IItemResolver[] { this };
        }

        protected override Video Resolve(ItemResolveArgs args)
        {
            return ResolveVideo<Video>(args, true);
        }

        /// <summary>
        /// Gets the resolvers for the extra type.
        /// </summary>
        /// <param name="extraType">The extra type.</param>
        /// <returns>The resolvers for the extra type.</returns>
        public IItemResolver[]? GetResolversForExtraType(ExtraType extraType) => extraType switch
        {
            ExtraType.Trailer => _trailerResolvers,
            // For audio we'll have to rely on the AudioResolver, which is a "built-in"
            ExtraType.ThemeSong => null,
            _ => _videoResolvers
        };

        public bool TryGetExtraTypeForOwner(string path, VideoFileInfo ownerVideoFileInfo, [NotNullWhen(true)] out ExtraType? extraType)
        {
            var extraResult = GetExtraInfo(path, _namingOptions);
            if (extraResult.ExtraType is null)
            {
                extraType = null;
                return false;
            }

            var cleanDateTimeResult = CleanDateTimeParser.Clean(Path.GetFileNameWithoutExtension(path), _namingOptions.CleanDateTimeRegexes);
            var name = cleanDateTimeResult.Name;
            var year = cleanDateTimeResult.Year;

            var parentDir = ownerVideoFileInfo.IsDirectory ? ownerVideoFileInfo.Path : Path.GetDirectoryName(ownerVideoFileInfo.Path.AsSpan());

            var trimmedFileNameWithoutExtension = TrimFilenameDelimiters(ownerVideoFileInfo.FileNameWithoutExtension, _namingOptions.VideoFlagDelimiters);
            var trimmedVideoInfoName = TrimFilenameDelimiters(ownerVideoFileInfo.Name, _namingOptions.VideoFlagDelimiters);
            var trimmedExtraFileName = TrimFilenameDelimiters(name, _namingOptions.VideoFlagDelimiters);

            // first check filenames
            bool isValid = StartsWith(trimmedExtraFileName, trimmedFileNameWithoutExtension)
                           || (StartsWith(trimmedExtraFileName, trimmedVideoInfoName) && year == ownerVideoFileInfo.Year);

            if (!isValid)
            {
                // When the extra rule type is DirectoryName we must go one level higher to get the "real" dir name
                var currentParentDir = extraResult.Rule?.RuleType == ExtraRuleType.DirectoryName
                    ? Path.GetDirectoryName(Path.GetDirectoryName(path.AsSpan()))
                    : Path.GetDirectoryName(path.AsSpan());

                isValid = !currentParentDir.IsEmpty && !parentDir.IsEmpty && currentParentDir.Equals(parentDir, StringComparison.OrdinalIgnoreCase);
            }

            extraType = extraResult.ExtraType;
            return isValid;
        }

        private static ReadOnlySpan<char> TrimFilenameDelimiters(ReadOnlySpan<char> name, ReadOnlySpan<char> videoFlagDelimiters)
        {
            return name.IsEmpty ? name : name.TrimEnd().TrimEnd(videoFlagDelimiters).TrimEnd();
        }

        private static bool StartsWith(ReadOnlySpan<char> fileName, ReadOnlySpan<char> baseName)
        {
            return !baseName.IsEmpty && fileName.StartsWith(baseName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
