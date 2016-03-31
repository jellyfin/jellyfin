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

            var random = new Random(Environment.TickCount + Guid.NewGuid().GetHashCode());

            var candidates = new List<ItemWithTrailer>();

            var itemPeople = _libraryManager.GetPeople(item);
            var allPeople = _libraryManager.GetPeople(new InternalPeopleQuery
            {
                AppearsInItemId = item.Id
            });

            var trailerTypes = new List<TrailerType>();

            if (config.EnableIntrosFromMoviesInLibrary)
            {
                trailerTypes.Add(TrailerType.LocalTrailer);
            }

            if (IsSupporter)
            {
                if (config.EnableIntrosFromUpcomingTrailers)
                {
                    trailerTypes.Add(TrailerType.ComingSoonToTheaters);
                }
                if (config.EnableIntrosFromUpcomingDvdMovies)
                {
                    trailerTypes.Add(TrailerType.ComingSoonToDvd);
                }
                if (config.EnableIntrosFromUpcomingStreamingMovies)
                {
                    trailerTypes.Add(TrailerType.ComingSoonToStreaming);
                }
                if (config.EnableIntrosFromSimilarMovies)
                {
                    trailerTypes.Add(TrailerType.Archive);
                }
            }

            if (trailerTypes.Count > 0)
            {
                var excludeTrailerTypes = Enum.GetNames(typeof(TrailerType))
                        .Select(i => (TrailerType)Enum.Parse(typeof(TrailerType), i, true))
                        .Except(trailerTypes)
                        .ToArray();

                var trailerResult = _libraryManager.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { typeof(Trailer).Name },
                    ExcludeTrailerTypes = excludeTrailerTypes
                });

                candidates.AddRange(trailerResult.Select(i => new ItemWithTrailer
                {
                    Item = i,
                    Type = i.SourceType == SourceType.Channel ? ItemWithTrailerType.ChannelTrailer : ItemWithTrailerType.ItemWithTrailer,
                    User = user,
                    WatchingItem = item,
                    WatchingItemPeople = itemPeople,
                    AllPeople = allPeople,
                    Random = random,
                    LibraryManager = _libraryManager
                }));
            } 

            return GetResult(item, candidates, config, ratingLevel);
        }

        private IEnumerable<IntroInfo> GetResult(BaseItem item, IEnumerable<ItemWithTrailer> candidates, CinemaModeConfiguration config, int? ratingLevel)
        {
            var customIntros = !string.IsNullOrWhiteSpace(config.CustomIntroPath) ?
                GetCustomIntros(config) :
                new List<IntroInfo>();

            var mediaInfoIntros = !string.IsNullOrWhiteSpace(config.MediaInfoIntroPath) ?
                GetMediaInfoIntros(config, item) :
                new List<IntroInfo>();

            var trailerLimit = config.TrailerLimit;

            // Avoid implicitly captured closure
            return candidates.Where(i =>
            {
                if (config.EnableIntrosParentalControl && !FilterByParentalRating(ratingLevel, i.Item))
                {
                    return false;
                }

                if (!config.EnableIntrosForWatchedContent && i.IsPlayed)
                {
                    return false;
                }
                return !IsDuplicate(item, i.Item);
            })
                .OrderByDescending(i => i.Score)
                .ThenBy(i => Guid.NewGuid())
                .ThenByDescending(i => i.IsPlayed ? 0 : 1)
                .Select(i => i.IntroInfo)
                .Take(trailerLimit)
                .Concat(customIntros.Take(1))
                .Concat(mediaInfoIntros);
        }

        private bool IsDuplicate(BaseItem playingContent, BaseItem test)
        {
            var id = playingContent.GetProviderId(MetadataProviders.Imdb);
            if (!string.IsNullOrWhiteSpace(id) && string.Equals(id, test.GetProviderId(MetadataProviders.Imdb), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            id = playingContent.GetProviderId(MetadataProviders.Tmdb);
            if (!string.IsNullOrWhiteSpace(id) && string.Equals(id, test.GetProviderId(MetadataProviders.Tmdb), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
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
                .Where(i => IsMatch(i.Path, codec));
        }

        private IEnumerable<IntroInfo> GetMediaInfoIntrosByAudioStream(List<IntroInfo> allIntros, MediaStream stream)
        {
            var codec = stream.Codec;

            if (string.IsNullOrWhiteSpace(codec))
            {
                return new List<IntroInfo>();
            }

            return allIntros
                .Where(i => IsAudioMatch(i.Path, stream));
        }

        private IEnumerable<IntroInfo> GetMediaInfoIntrosByTags(List<IntroInfo> allIntros, List<string> tags)
        {
            return allIntros
                .Where(i => tags.Any(t => IsMatch(i.Path, t)));
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

        private bool FilterByParentalRating(int? ratingLevel, BaseItem item)
        {
            // Only content rated same or lower
            if (ratingLevel.HasValue)
            {
                var level = string.IsNullOrWhiteSpace(item.OfficialRating)
                    ? (int?)null
                    : _localization.GetRatingLevel(item.OfficialRating);

                return level.HasValue && level.Value <= ratingLevel.Value;
            }

            return true;
        }

        internal static int GetSimiliarityScore(BaseItem item1, List<PersonInfo> item1People, List<PersonInfo> allPeople, BaseItem item2, Random random, ILibraryManager libraryManager)
        {
            var points = 0;

            if (!string.IsNullOrEmpty(item1.OfficialRating) && string.Equals(item1.OfficialRating, item2.OfficialRating, StringComparison.OrdinalIgnoreCase))
            {
                points += 10;
            }

            // Find common genres
            points += item1.Genres.Where(i => item2.Genres.Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 10);

            // Find common tags
            points += GetTags(item1).Where(i => GetTags(item2).Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 10);

            // Find common keywords
            points += GetKeywords(item1).Where(i => GetKeywords(item2).Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 10);

            // Find common studios
            points += item1.Studios.Where(i => item2.Studios.Contains(i, StringComparer.OrdinalIgnoreCase)).Sum(i => 5);

            var item2PeopleNames = allPeople.Where(i => i.ItemId == item2.Id)
                .Select(i => i.Name)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .DistinctNames()
                .ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            points += item1People.Where(i => item2PeopleNames.ContainsKey(i.Name)).Sum(i =>
            {
                if (string.Equals(i.Type, PersonType.Director, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Director, StringComparison.OrdinalIgnoreCase))
                {
                    return 5;
                }
                if (string.Equals(i.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Actor, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                if (string.Equals(i.Type, PersonType.Composer, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Composer, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                if (string.Equals(i.Type, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase))
                {
                    return 3;
                }
                if (string.Equals(i.Type, PersonType.Writer, StringComparison.OrdinalIgnoreCase) || string.Equals(i.Role, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
                {
                    return 2;
                }

                return 1;
            });

            // Add some randomization so that you're not always seeing the same ones for a given movie
            points += random.Next(0, 50);

            return points;
        }

        private static IEnumerable<string> GetTags(BaseItem item)
        {
            var hasTags = item as IHasTags;
            if (hasTags != null)
            {
                return hasTags.Tags;
            }

            return new List<string>();
        }

        private static IEnumerable<string> GetKeywords(BaseItem item)
        {
            var hasTags = item as IHasKeywords;
            if (hasTags != null)
            {
                return hasTags.Keywords;
            }

            return new List<string>();
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
            internal User User;
            internal BaseItem WatchingItem;
            internal List<PersonInfo> WatchingItemPeople;
            internal List<PersonInfo> AllPeople;
            internal Random Random;
            internal ILibraryManager LibraryManager;

            private bool? _isPlayed;
            public bool IsPlayed
            {
                get
                {
                    if (!_isPlayed.HasValue)
                    {
                        _isPlayed = Item.IsPlayed(User);
                    }
                    return _isPlayed.Value;
                }
            }

            private int? _score;
            public int Score
            {
                get
                {
                    if (!_score.HasValue)
                    {
                        _score = GetSimiliarityScore(WatchingItem, WatchingItemPeople, AllPeople, Item, Random, LibraryManager);
                    }
                    return _score.Value;
                }
            }

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
