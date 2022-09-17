#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Lyrics;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Lyric
{
    public class LyricManager : ILyricManager
    {
        private readonly ILogger<LyricManager> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _monitor;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly ILocalizationManager _localization;

        private IEnumerable<ILyricProvider> _lyricProviders;

        public LyricManager(
            ILogger<LyricManager> logger,
            IFileSystem fileSystem,
            ILibraryMonitor monitor,
            IMediaSourceManager mediaSourceManager,
            ILocalizationManager localizationManager,
            IEnumerable<ILyricProvider> lyricProviders)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _monitor = monitor;
            _mediaSourceManager = mediaSourceManager;
            _localization = localizationManager;
            _lyricProviders = lyricProviders;
        }

        /// <inheritdoc />
        public LyricResponse GetLyrics(BaseItem item)
        {
            foreach (ILyricProvider provider in _lyricProviders)
            {
                var results = provider.GetLyrics(item);
                if (results is not null)
                {
                    return results;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public bool HasLyricFile(BaseItem item)
        {
            foreach (ILyricProvider provider in _lyricProviders)
            {
                if (item is null)
                {
                    continue;
                }

                if (LyricInfo.GetLyricFilePath(provider, item.Path) is not null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
