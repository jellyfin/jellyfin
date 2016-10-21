using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonIO;
using MoreLinq;

namespace MediaBrowser.Server.Implementations.Intros
{
    public class DefaultIntroProvider : IIntroProvider
    {
        private readonly ISecurityManager _security;
        private readonly ILocalizationManager _localization;
        private readonly IConfigurationManager _serverConfig;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly IMediaSourceManager _mediaSourceManager;

        public DefaultIntroProvider(ISecurityManager security, ILocalizationManager localization, IConfigurationManager serverConfig, ILibraryManager libraryManager, IFileSystem fileSystem, IMediaSourceManager mediaSourceManager)
        {
            _security = security;
            _localization = localization;
            _serverConfig = serverConfig;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _mediaSourceManager = mediaSourceManager;
        }

        public async Task<IEnumerable<IntroInfo>> GetIntros(BaseItem item, User user)
        {
            var config = GetOptions();

            if (item is Movie)
            {
                if (!config.EnableIntrosForMovies)
                {
                    return new List<IntroInfo>();
                }
            }
            else if (item is Episode)
            {
                if (!config.EnableIntrosForEpisodes)
                {
                    return new List<IntroInfo>();
                }
            }
            else
            {
                return new List<IntroInfo>();
            }

            var ratingLevel = string.IsNullOrWhiteSpace(item.OfficialRating)
                ? null
                : _localization.GetRatingLevel(item.OfficialRating);

            var candidates = new List<ItemWithTrailer>();

            var trailerTypes = new List<TrailerType>();
            var sourceTypes = new List<SourceType>();

            if (config.EnableIntrosFromMoviesInLibrary)
            {
                trailerTypes.Add(TrailerType.LocalTrailer);
                sourceTypes.Add(SourceType.Library);
            }

            if (IsSupporter)
            {
                if (config.EnableIntrosFromUpcomingTrailers)
                {
                    trailerTypes.Add(TrailerType.ComingSoonToTheaters);
                    sourceTypes.Clear();
                }
                if (config.EnableIntrosFromUpcomingDvdMovies)
                {
                    trailerTypes.Add(TrailerType.ComingSoonToDvd);
                    sourceTypes.Clear();
                }
                if (config.EnableIntrosFromUpcomingStreamingMovies)
                {
                    trailerTypes.Add(TrailerType.ComingSoonToStreaming);
                    sourceTypes.Clear();
                }
                if (config.EnableIntrosFromSimilarMovies)
                {
                    trailerTypes.Add(TrailerType.Archive);
                    sourceTypes.Clear();
                }
            }

            if (trailerTypes.Count > 0)
            {
                var trailerResult = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { typeof(Trailer).Name },
                    TrailerTypes = trailerTypes.ToArray(),
                    SimilarTo = item,
                    IsPlayed = config.EnableIntrosForWatchedContent ? (bool?)null : false,
                    MaxParentalRating = config.EnableIntrosParentalControl ? ratingLevel : null,
                    BlockUnratedItems = config.EnableIntrosParentalControl ? new[] { UnratedItem.Trailer } : new UnratedItem[] { },

                    // Account for duplicates by imdb id, since the database doesn't support this yet
                    Limit = config.TrailerLimit * 2,
                    SourceTypes = sourceTypes.ToArray()

                }).Where(i => string.IsNullOrWhiteSpace(i.GetProviderId(MetadataProviders.Imdb)) || !string.Equals(i.GetProviderId(MetadataProviders.Imdb), item.GetProviderId(MetadataProviders.Imdb), StringComparison.OrdinalIgnoreCase)).Take(config.TrailerLimit);

                candidates.AddRange(trailerResult.Select(i => new ItemWithTrailer
                {
                    Item = i,
                    Type = i.SourceType == SourceType.Channel ? ItemWithTrailerType.ChannelTrailer : ItemWithTrailerType.ItemWithTrailer,
                    LibraryManager = _libraryManager
                }));
            }

            return GetResult(item, candidates, config);
        }

        private IEnumerable<IntroInfo> GetResult(BaseItem item, IEnumerable<ItemWithTrailer> candidates, CinemaModeConfiguration config)
        {
            var customIntros = !string.IsNullOrWhiteSpace(config.CustomIntroPath) ?
                GetCustomIntros(config) :
                new List<IntroInfo>();

            var mediaInfoIntros = !string.IsNullOrWhiteSpace(config.MediaInfoIntroPath) ?
                GetMediaInfoIntros(config, item) :
                new List<IntroInfo>();

            // Avoid implicitly captured closure
            return candidates.Select(i => i.IntroInfo)
                .Concat(customIntros.Take(1))
                .Concat(mediaInfoIntros);
        }

        private CinemaModeConfiguration GetOptions()
        {
            return _serverConfig.GetConfiguration<CinemaModeConfiguration>("cinemamode");
        }

        private List<IntroInfo> GetCustomIntros(CinemaModeConfiguration options)
        {
            try
            {
                return GetCustomIntroFiles(options, true, false)
                    .OrderBy(i => Guid.NewGuid())
                    .Select(i => new IntroInfo
                    {
                        Path = i

                    }).ToList();
            }
            catch (IOException)
            {
                return new List<IntroInfo>();
            }
        }

        private IEnumerable<IntroInfo> GetMediaInfoIntros(CinemaModeConfiguration options, BaseItem item)
        {
            try
            {
                var hasMediaSources = item as IHasMediaSources;

                if (hasMediaSources == null)
                {
                    return new List<IntroInfo>();
                }

                var mediaSource = _mediaSourceManager.GetStaticMediaSources(hasMediaSources, false)
                    .FirstOrDefault();

                if (mediaSource == null)
                {
                    return new List<IntroInfo>();
                }

                var videoStream = mediaSource.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video);
                var audioStream = mediaSource.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

                var allIntros = GetCustomIntroFiles(options, false, true)
                    .OrderBy(i => Guid.NewGuid())
                    .Select(i => new IntroInfo
                    {
                        Path = i

                    }).ToList();

                var returnResult = new List<IntroInfo>();

                if (videoStream != null)
                {
                    returnResult.AddRange(GetMediaInfoIntrosByVideoStream(allIntros, videoStream).Take(1));
                }

                if (audioStream != null)
                {
                    returnResult.AddRange(GetMediaInfoIntrosByAudioStream(allIntros, audioStream).Take(1));
                }

                returnResult.AddRange(GetMediaInfoIntrosByTags(allIntros, item.Tags).Take(1));

                return returnResult.DistinctBy(i => i.Path, StringComparer.OrdinalIgnoreCase);
            }
            catch (IOException)
            {
                return new List<IntroInfo>();
            }
        }

        private IEnumerable<IntroInfo> GetMediaInfoIntrosByVideoStream(List<IntroInfo> allIntros, MediaStream stream)
        {
            var codec = stream.Codec;

            if (string.IsNullOrWhiteSpace(codec))
            {
                return new List<IntroInfo>();
            }

            return allIntros
                .Where(i => IsMatch(i.Path, codec))
                .OrderBy(i => Guid.NewGuid());
        }

        private IEnumerable<IntroInfo> GetMediaInfoIntrosByAudioStream(List<IntroInfo> allIntros, MediaStream stream)
        {
            var codec = stream.Codec;

            if (string.IsNullOrWhiteSpace(codec))
            {
                return new List<IntroInfo>();
            }

            return allIntros
                .Where(i => IsAudioMatch(i.Path, stream))
                .OrderBy(i => Guid.NewGuid());
        }

        private IEnumerable<IntroInfo> GetMediaInfoIntrosByTags(List<IntroInfo> allIntros, List<string> tags)
        {
            return allIntros
                .Where(i => tags.Any(t => IsMatch(i.Path, t)))
                .OrderBy(i => Guid.NewGuid());
        }

        private bool IsMatch(string file, string attribute)
        {
            var filename = Path.GetFileNameWithoutExtension(file) ?? string.Empty;
            filename = Normalize(filename);

            if (string.IsNullOrWhiteSpace(filename))
            {
                return false;
            }

            attribute = Normalize(attribute);
            if (string.IsNullOrWhiteSpace(attribute))
            {
                return false;
            }

            return string.Equals(filename, attribute, StringComparison.OrdinalIgnoreCase);
        }

        private string Normalize(string value)
        {
            return value;
        }

        private bool IsAudioMatch(string path, MediaStream stream)
        {
            if (!string.IsNullOrWhiteSpace(stream.Codec))
            {
                if (IsMatch(path, stream.Codec))
                {
                    return true;
                }
            }
            if (!string.IsNullOrWhiteSpace(stream.Profile))
            {
                if (IsMatch(path, stream.Profile))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<string> GetCustomIntroFiles(CinemaModeConfiguration options, bool enableCustomIntros, bool enableMediaInfoIntros)
        {
            var list = new List<string>();

            if (enableCustomIntros && !string.IsNullOrWhiteSpace(options.CustomIntroPath))
            {
                list.AddRange(_fileSystem.GetFilePaths(options.CustomIntroPath, true)
                    .Where(_libraryManager.IsVideoFile));
            }

            if (enableMediaInfoIntros && !string.IsNullOrWhiteSpace(options.MediaInfoIntroPath))
            {
                list.AddRange(_fileSystem.GetFilePaths(options.MediaInfoIntroPath, true)
                    .Where(_libraryManager.IsVideoFile));
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<string> GetAllIntroFiles()
        {
            return GetCustomIntroFiles(GetOptions(), true, true);
        }

        private bool IsSupporter
        {
            get { return _security.IsMBSupporter; }
        }

        public string Name
        {
            get { return "Default"; }
        }

        internal class ItemWithTrailer
        {
            internal BaseItem Item;
            internal ItemWithTrailerType Type;
            internal ILibraryManager LibraryManager;

            public IntroInfo IntroInfo
            {
                get
                {
                    var id = Item.Id;

                    if (Type == ItemWithTrailerType.ItemWithTrailer)
                    {
                        var hasTrailers = Item as IHasTrailers;

                        if (hasTrailers != null)
                        {
                            id = hasTrailers.LocalTrailerIds.FirstOrDefault();
                        }
                    }
                    return new IntroInfo
                    {
                        ItemId = id
                    };
                }
            }
        }

        internal enum ItemWithTrailerType
        {
            ChannelTrailer,
            ItemWithTrailer
        }
    }

    public class CinemaModeConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new[]
            {
                new ConfigurationStore
                {
                     ConfigurationType = typeof(CinemaModeConfiguration),
                     Key = "cinemamode"
                }
            };
        }
    }

}
