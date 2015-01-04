using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Class LuceneSearchEngine
    /// http://www.codeproject.com/Articles/320219/Lucene-Net-ultra-fast-search-for-MVC-or-WebForms
    /// </summary>
    public class SearchEngine : ISearchEngine
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        public SearchEngine(ILogManager logManager, ILibraryManager libraryManager, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;

            _logger = logManager.GetLogger("Lucene");
        }

        public async Task<QueryResult<SearchHintInfo>> GetSearchHints(SearchQuery query)
        {
            IEnumerable<BaseItem> inputItems;

            if (string.IsNullOrWhiteSpace(query.UserId))
            {
                inputItems = _libraryManager.RootFolder.RecursiveChildren;
            }
            else
            {
                var user = _userManager.GetUserById(query.UserId);

                inputItems = user.RootFolder.GetRecursiveChildren(user, true);
            }


            inputItems = inputItems.Where(i => !(i is ICollectionFolder));

            inputItems = _libraryManager.ReplaceVideosWithPrimaryVersions(inputItems);

            var results = await GetSearchHints(inputItems, query).ConfigureAwait(false);

            // Include item types
            if (query.IncludeItemTypes.Length > 0)
            {
                results = results.Where(f => query.IncludeItemTypes.Contains(f.Item.GetType().Name, StringComparer.OrdinalIgnoreCase));
            }

            var searchResultArray = results.ToArray();
            results = searchResultArray;

            var count = searchResultArray.Length;

            if (query.StartIndex.HasValue)
            {
                results = results.Skip(query.StartIndex.Value);
            }

            if (query.Limit.HasValue)
            {
                results = results.Take(query.Limit.Value);
            }

            return new QueryResult<SearchHintInfo>
            {
                TotalRecordCount = count,

                Items = results.ToArray()
            };
        }

        /// <summary>
        /// Gets the search hints.
        /// </summary>
        /// <param name="inputItems">The input items.</param>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{SearchHintResult}.</returns>
        /// <exception cref="System.ArgumentNullException">searchTerm</exception>
        private Task<IEnumerable<SearchHintInfo>> GetSearchHints(IEnumerable<BaseItem> inputItems, SearchQuery query)
        {
            var searchTerm = query.SearchTerm;

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentNullException("searchTerm");
            }

            var terms = GetWords(searchTerm);

            var hints = new List<Tuple<BaseItem, string, int>>();

            var items = inputItems.Where(i => !(i is MusicArtist)).ToList();

            if (query.IncludeMedia)
            {
                // Add search hints based on item name
                hints.AddRange(items.Where(i => !string.IsNullOrWhiteSpace(i.Name) && IncludeInSearch(i)).Select(item =>
                {
                    var index = GetIndex(item.Name, searchTerm, terms);

                    return new Tuple<BaseItem, string, int>(item, index.Item1, index.Item2);
                }));
            }

            if (query.IncludeArtists)
            {
                // Find artists
                var artists = items.OfType<Audio>()
                    .SelectMany(i => i.AllArtists)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var item in artists)
                {
                    var index = GetIndex(item, searchTerm, terms);

                    if (index.Item2 != -1)
                    {
                        try
                        {
                            var artist = _libraryManager.GetArtist(item);

                            hints.Add(new Tuple<BaseItem, string, int>(artist, index.Item1, index.Item2));
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error getting {0}", ex, item);
                        }
                    }
                }
            }

            if (query.IncludeGenres)
            {
                // Find genres, from non-audio items
                var genres = items.Where(i => !(i is IHasMusicGenres) && !(i is Game))
                    .SelectMany(i => i.Genres)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var item in genres)
                {
                    var index = GetIndex(item, searchTerm, terms);

                    if (index.Item2 != -1)
                    {
                        try
                        {
                            var genre = _libraryManager.GetGenre(item);

                            hints.Add(new Tuple<BaseItem, string, int>(genre, index.Item1, index.Item2));
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error getting {0}", ex, item);
                        }
                    }
                }

                // Find music genres
                var musicGenres = items.Where(i => i is IHasMusicGenres)
                    .SelectMany(i => i.Genres)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var item in musicGenres)
                {
                    var index = GetIndex(item, searchTerm, terms);

                    if (index.Item2 != -1)
                    {
                        try
                        {
                            var genre = _libraryManager.GetMusicGenre(item);

                            hints.Add(new Tuple<BaseItem, string, int>(genre, index.Item1, index.Item2));
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error getting {0}", ex, item);
                        }
                    }
                }

                // Find music genres
                var gameGenres = items.OfType<Game>()
                    .SelectMany(i => i.Genres)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var item in gameGenres)
                {
                    var index = GetIndex(item, searchTerm, terms);

                    if (index.Item2 != -1)
                    {
                        try
                        {
                            var genre = _libraryManager.GetGameGenre(item);

                            hints.Add(new Tuple<BaseItem, string, int>(genre, index.Item1, index.Item2));
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error getting {0}", ex, item);
                        }
                    }
                }
            }

            if (query.IncludeStudios)
            {
                // Find studios
                var studios = items.SelectMany(i => i.Studios)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var item in studios)
                {
                    var index = GetIndex(item, searchTerm, terms);

                    if (index.Item2 != -1)
                    {
                        try
                        {
                            var studio = _libraryManager.GetStudio(item);

                            hints.Add(new Tuple<BaseItem, string, int>(studio, index.Item1, index.Item2));
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error getting {0}", ex, item);
                        }
                    }
                }
            }

            if (query.IncludePeople)
            {
                // Find persons
                var persons = items.SelectMany(i => i.People)
                    .Select(i => i.Name)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var item in persons)
                {
                    var index = GetIndex(item, searchTerm, terms);

                    if (index.Item2 != -1)
                    {
                        try
                        {
                            var person = _libraryManager.GetPerson(item);

                            hints.Add(new Tuple<BaseItem, string, int>(person, index.Item1, index.Item2));
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error getting {0}", ex, item);
                        }
                    }
                }
            }

            var returnValue = hints.Where(i => i.Item3 >= 0).OrderBy(i => i.Item3).Select(i => new SearchHintInfo
            {
                Item = i.Item1,
                MatchedTerm = i.Item2
            });

            return Task.FromResult(returnValue);
        }

        private bool IncludeInSearch(BaseItem item)
        {
            var episode = item as Episode;

            if (episode != null)
            {
                if (episode.IsVirtualUnaired || episode.IsMissingEpisode)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the index.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="searchInput">The search input.</param>
        /// <param name="searchWords">The search input.</param>
        /// <returns>System.Int32.</returns>
        private Tuple<string, int> GetIndex(string input, string searchInput, List<string> searchWords)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException("input");
            }

            if (string.Equals(input, searchInput, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, int>(searchInput, 0);
            }

            var index = input.IndexOf(searchInput, StringComparison.OrdinalIgnoreCase);

            if (index == 0)
            {
                return new Tuple<string, int>(searchInput, 1);
            }
            if (index > 0)
            {
                return new Tuple<string, int>(searchInput, 2);
            }

            var items = GetWords(input);

            for (var i = 0; i < searchWords.Count; i++)
            {
                var searchTerm = searchWords[i];

                for (var j = 0; j < items.Count; j++)
                {
                    var item = items[j];

                    if (string.Equals(item, searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        return new Tuple<string, int>(searchTerm, 3 + (i + 1) * (j + 1));
                    }

                    index = item.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);

                    if (index == 0)
                    {
                        return new Tuple<string, int>(searchTerm, 4 + (i + 1) * (j + 1));
                    }
                    if (index > 0)
                    {
                        return new Tuple<string, int>(searchTerm, 5 + (i + 1) * (j + 1));
                    }
                }
            }
            return new Tuple<string, int>(null, -1);
        }

        /// <summary>
        /// Gets the words.
        /// </summary>
        /// <param name="term">The term.</param>
        /// <returns>System.String[][].</returns>
        private List<string> GetWords(string term)
        {
            var stoplist = GetStopList().ToList();

            return term.Split()
                .Where(i => !string.IsNullOrWhiteSpace(i) && !stoplist.Contains(i, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        private IEnumerable<string> GetStopList()
        {
            return new[]
            {
                "the",
                "a",
                "of",
                "an"
            };
        }
    }
}
