using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.MediaInfo
{
    public class SubtitleScheduledTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _config;
        private readonly ISubtitleManager _subtitleManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly ILogger _logger;

        public SubtitleScheduledTask(ILibraryManager libraryManager, IServerConfigurationManager config, ISubtitleManager subtitleManager, ILogger logger, IMediaSourceManager mediaSourceManager)
        {
            _libraryManager = libraryManager;
            _config = config;
            _subtitleManager = subtitleManager;
            _logger = logger;
            _mediaSourceManager = mediaSourceManager;
        }

        public string Name
        {
            get { return "Download missing subtitles"; }
        }

        public string Description
        {
            get { return "Searches the internet for missing subtitles based on metadata configuration."; }
        }

        public string Category
        {
            get { return "Library"; }
        }

        private SubtitleOptions GetOptions()
        {
            return _config.GetConfiguration<SubtitleOptions>("subtitles");
        }

        public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var options = GetOptions();

            var videos = _libraryManager.RootFolder
                .GetRecursiveChildren(i =>
                {
                    if (!(i is Video))
                    {
                        return false;
                    }

                    if (i.LocationType == LocationType.Remote || i.LocationType == LocationType.Virtual)
                    {
                        return false;
                    }

                    return (options.DownloadEpisodeSubtitles &&
                            i is Episode) ||
                           (options.DownloadMovieSubtitles &&
                            i is Movie);
                })
                .Cast<Video>()
                .ToList();

            var numComplete = 0;

            foreach (var video in videos)
            {
                try
                {
                    await DownloadSubtitles(video, options, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error downloading subtitles for {0}", ex, video.Path);
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= videos.Count;

                progress.Report(100 * percent);
            }
        }

        private async Task DownloadSubtitles(Video video, SubtitleOptions options, CancellationToken cancellationToken)
        {
            if ((options.DownloadEpisodeSubtitles &&
                video is Episode) ||
                (options.DownloadMovieSubtitles &&
                video is Movie))
            {
                var mediaStreams = _mediaSourceManager.GetStaticMediaSources(video, false).First().MediaStreams;

                var downloadedLanguages = await new SubtitleDownloader(_logger,
                    _subtitleManager)
                    .DownloadSubtitles(video,
                    mediaStreams,
                    options.SkipIfEmbeddedSubtitlesPresent,
                    options.SkipIfAudioTrackMatches,
                    options.RequirePerfectMatch,
                    options.DownloadLanguages,
                    cancellationToken).ConfigureAwait(false);

                // Rescan
                if (downloadedLanguages.Count > 0)
                {
                    await video.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public IEnumerable<ITaskTrigger> GetDefaultTriggers()
        {
            return new ITaskTrigger[]
                {
                new IntervalTrigger{ Interval = TimeSpan.FromHours(8)}
                };
        }
    }
}
