using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Providers.MediaInfo
{
    public class SubtitleScheduledTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;
        private readonly ISubtitleManager _subtitleManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;
        private readonly ILocalizationManager _localization;

        public SubtitleScheduledTask(
            ILibraryManager libraryManager,
            IJsonSerializer json,
            IServerConfigurationManager config,
            ISubtitleManager subtitleManager,
            ILogger<SubtitleScheduledTask> logger,
            IMediaSourceManager mediaSourceManager,
            ILocalizationManager localization)
        {
            _libraryManager = libraryManager;
            _config = config;
            _subtitleManager = subtitleManager;
            _logger = logger;
            _mediaSourceManager = mediaSourceManager;
            _json = json;
            _localization = localization;
        }

        private SubtitleOptions GetOptions()
        {
            return _config.GetConfiguration<SubtitleOptions>("subtitles");
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var options = GetOptions();

            var types = new[] { "Episode", "Movie" };

            var dict = new Dictionary<Guid, BaseItem>();

            foreach (var library in _libraryManager.RootFolder.Children.ToList())
            {
                var libraryOptions = _libraryManager.GetLibraryOptions(library);

                string[] subtitleDownloadLanguages;
                bool SkipIfEmbeddedSubtitlesPresent;
                bool SkipIfAudioTrackMatches;
                bool RequirePerfectMatch;

                if (libraryOptions.SubtitleDownloadLanguages == null)
                {
                    subtitleDownloadLanguages = options.DownloadLanguages;
                    SkipIfEmbeddedSubtitlesPresent = options.SkipIfEmbeddedSubtitlesPresent;
                    SkipIfAudioTrackMatches = options.SkipIfAudioTrackMatches;
                    RequirePerfectMatch = options.RequirePerfectMatch;
                }
                else
                {
                    subtitleDownloadLanguages = libraryOptions.SubtitleDownloadLanguages;
                    SkipIfEmbeddedSubtitlesPresent = libraryOptions.SkipSubtitlesIfEmbeddedSubtitlesPresent;
                    SkipIfAudioTrackMatches = libraryOptions.SkipSubtitlesIfAudioTrackMatches;
                    RequirePerfectMatch = libraryOptions.RequirePerfectSubtitleMatch;
                }

                foreach (var lang in subtitleDownloadLanguages)
                {
                    var query = new InternalItemsQuery
                    {
                        MediaTypes = new string[] { MediaType.Video },
                        IsVirtualItem = false,
                        IncludeItemTypes = types,
                        DtoOptions = new DtoOptions(true),
                        SourceTypes = new[] { SourceType.Library },
                        Parent = library,
                        Recursive = true
                    };

                    if (SkipIfAudioTrackMatches)
                    {
                        query.HasNoAudioTrackWithLanguage = lang;
                    }

                    if (SkipIfEmbeddedSubtitlesPresent)
                    {
                        // Exclude if it already has any subtitles of the same language
                        query.HasNoSubtitleTrackWithLanguage = lang;
                    }
                    else
                    {
                        // Exclude if it already has external subtitles of the same language
                        query.HasNoExternalSubtitleTrackWithLanguage = lang;
                    }

                    var videosByLanguage = _libraryManager.GetItemList(query);

                    foreach (var video in videosByLanguage)
                    {
                        dict[video.Id] = video;
                    }
                }
            }

            var videos = dict.Values.ToList();
            if (videos.Count == 0)
            {
                return;
            }

            var numComplete = 0;

            foreach (var video in videos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await DownloadSubtitles(video as Video, options, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error downloading subtitles for {Path}", video.Path);
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= videos.Count;

                progress.Report(100 * percent);
            }
        }

        private async Task<bool> DownloadSubtitles(Video video, SubtitleOptions options, CancellationToken cancellationToken)
        {
            var mediaStreams = video.GetMediaStreams();

            var libraryOptions = _libraryManager.GetLibraryOptions(video);

            string[] subtitleDownloadLanguages;
            bool SkipIfEmbeddedSubtitlesPresent;
            bool SkipIfAudioTrackMatches;
            bool RequirePerfectMatch;

            if (libraryOptions.SubtitleDownloadLanguages == null)
            {
                subtitleDownloadLanguages = options.DownloadLanguages;
                SkipIfEmbeddedSubtitlesPresent = options.SkipIfEmbeddedSubtitlesPresent;
                SkipIfAudioTrackMatches = options.SkipIfAudioTrackMatches;
                RequirePerfectMatch = options.RequirePerfectMatch;
            }
            else
            {
                subtitleDownloadLanguages = libraryOptions.SubtitleDownloadLanguages;
                SkipIfEmbeddedSubtitlesPresent = libraryOptions.SkipSubtitlesIfEmbeddedSubtitlesPresent;
                SkipIfAudioTrackMatches = libraryOptions.SkipSubtitlesIfAudioTrackMatches;
                RequirePerfectMatch = libraryOptions.RequirePerfectSubtitleMatch;
            }

            var downloadedLanguages = await new SubtitleDownloader(_logger,
                _subtitleManager)
                .DownloadSubtitles(video,
                mediaStreams,
                SkipIfEmbeddedSubtitlesPresent,
                SkipIfAudioTrackMatches,
                RequirePerfectMatch,
                subtitleDownloadLanguages,
                libraryOptions.DisabledSubtitleFetchers,
                libraryOptions.SubtitleFetcherOrder,
                cancellationToken).ConfigureAwait(false);

            // Rescan
            if (downloadedLanguages.Count > 0)
            {
                await video.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                return false;
            }

            return true;
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return new[] {

                // Every so often
                new TaskTriggerInfo { Type = TaskTriggerInfo.TriggerInterval, IntervalTicks = TimeSpan.FromHours(24).Ticks}
            };
        }

        public string Name => _localization.GetLocalizedString("TaskDownloadMissingSubtitles");

        public string Description => _localization.GetLocalizedString("TaskDownloadMissingSubtitlesDescription");

        public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

        public string Key => "DownloadSubtitles";

        public bool IsHidden => false;

        public bool IsEnabled => true;

        public bool IsLogged => true;
    }
}
