using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Security;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.IO;

namespace MediaBrowser.Server.Implementations.Intros
{
    public class DefaultIntroProvider : IIntroProvider
    {
        private readonly ISecurityManager _security;
        private readonly IChannelManager _channelManager;
        private readonly ILocalizationManager _localization;
        private readonly IConfigurationManager _serverConfig;
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;

        public DefaultIntroProvider(ISecurityManager security, IChannelManager channelManager, ILocalizationManager localization, IConfigurationManager serverConfig, ILibraryManager libraryManager, IFileSystem fileSystem)
        {
            _security = security;
            _channelManager = channelManager;
            _localization = localization;
            _serverConfig = serverConfig;
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
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

            if (config.EnableIntrosFromMoviesInLibrary)
            {
                var inputItems = _libraryManager.GetItems(new InternalItemsQuery
                {
                    IncludeItemTypes = new[] { typeof(Movie).Name },

                    User = user

                }).Items;

                var itemsWithTrailers = inputItems
                    .Where(i =>
                    {
                        var hasTrailers = i as IHasTrailers;

                        if (hasTrailers != null && hasTrailers.LocalTrailerIds.Count > 0)
                        {
                            if (i is Movie)
                            {
                                return !IsDuplicate(item, i);
                            }
                        }
                        return false;
                    });

                candidates.AddRange(itemsWithTrailers.Select(i => new ItemWithTrailer
                {
                    Item = i,
                    Type = ItemWithTrailerType.ItemWithTrailer,
                    User = user,
                    WatchingItem = item,
                    WatchingItemPeople = itemPeople,
                    AllPeople = allPeople,
                    Random = random,
                    LibraryManager = _libraryManager
                }));
            }

            var trailerTypes = new List<TrailerType>();

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

            if (trailerTypes.Count > 0 && IsSupporter)
            {
                var channelTrailers = await _channelManager.GetAllMediaInternal(new AllChannelMediaQuery
                {
                    ContentTypes = new[] { ChannelMediaContentType.MovieExtra },
                    ExtraTypes = new[] { ExtraType.Trailer },
                    UserId = user.Id.ToString("N"),
                    TrailerTypes = trailerTypes.ToArray()

                }, CancellationToken.None);

                candidates.AddRange(channelTrailers.Items.Select(i => new ItemWithTrailer
                {
                    Item = i,
                    Type = ItemWithTrailerType.ChannelTrailer,
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
                GetCustomIntros(item) :
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
                .ThenByDescending(i => (i.IsPlayed ? 0 : 1))
                .Select(i => i.IntroInfo)
                .Take(trailerLimit)
                .Concat(customIntros.Take(1));
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

        private List<IntroInfo> GetCustomIntros(BaseItem item)
        {
            try
            {
                return GetCustomIntroFiles()
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

        private IEnumerable<string> GetCustomIntroFiles(CinemaModeConfiguration options = null)
        {
            options = options ?? GetOptions();

            if (string.IsNullOrWhiteSpace(options.CustomIntroPath))
            {
                return new List<string>();
            }

            return _fileSystem.GetFilePaths(options.CustomIntroPath, true)
                .Where(_libraryManager.IsVideoFile);
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
            return GetCustomIntroFiles();
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
            LibraryTrailer,
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
