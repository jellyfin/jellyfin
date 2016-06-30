using MediaBrowser.Common.Extensions;
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
            User user = null;

            if (string.IsNullOrWhiteSpace(query.UserId))
            {
            }
            else
            {
                user = _userManager.GetUserById(query.UserId);
            }

            var results = await GetSearchHints(query, user).ConfigureAwait(false);

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

        private void AddIfMissing(List<string> list, string value)
        {
            if (!list.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(value);
            }
        }

        /// <summary>
        /// Gets the search hints.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{SearchHintResult}.</returns>
        /// <exception cref="System.ArgumentNullException">searchTerm</exception>
        private Task<IEnumerable<SearchHintInfo>> GetSearchHints(SearchQuery query, User user)
        {
            var searchTerm = query.SearchTerm;

            if (searchTerm != null)
            {
                searchTerm = searchTerm.Trim().RemoveDiacritics();
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentNullException("searchTerm");
            }

            var terms = GetWords(searchTerm);

            var hints = new List<Tuple<BaseItem, string, int>>();

            var excludeItemTypes = new List<string>();
            var includeItemTypes = (query.IncludeItemTypes ?? new string[] { }).ToList();

            excludeItemTypes.Add(typeof(Year).Name);

            if (query.IncludeGenres && (includeItemTypes.Count == 0 || includeItemTypes.Contains("Genre", StringComparer.OrdinalIgnoreCase)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, typeof(Genre).Name);
                    AddIfMissing(includeItemTypes, typeof(GameGenre).Name);
                    AddIfMissing(includeItemTypes, typeof(MusicGenre).Name);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, typeof(Genre).Name);
                AddIfMissing(excludeItemTypes, typeof(GameGenre).Name);
                AddIfMissing(excludeItemTypes, typeof(MusicGenre).Name);
            }

            if (query.IncludePeople && (includeItemTypes.Count == 0 || includeItemTypes.Contains("People", StringComparer.OrdinalIgnoreCase) || includeItemTypes.Contains("Person", StringComparer.OrdinalIgnoreCase)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, typeof(Person).Name);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, typeof(Person).Name);
            }

            if (query.IncludeStudios && (includeItemTypes.Count == 0 || includeItemTypes.Contains("Studio", StringComparer.OrdinalIgnoreCase)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, typeof(Studio).Name);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, typeof(Studio).Name);
            }

            if (query.IncludeArtists && (includeItemTypes.Count == 0 || includeItemTypes.Contains("MusicArtist", StringComparer.OrdinalIgnoreCase)))
            {
                if (!query.IncludeMedia)
                {
                    AddIfMissing(includeItemTypes, typeof(MusicArtist).Name);
                }
            }
            else
            {
                AddIfMissing(excludeItemTypes, typeof(MusicArtist).Name);
            }

            AddIfMissing(excludeItemTypes, typeof(CollectionFolder).Name);

            var mediaItems = _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                NameContains = searchTerm,
                ExcludeItemTypes = excludeItemTypes.ToArray(),
                IncludeItemTypes = includeItemTypes.ToArray(),
                Limit = query.Limit,
                IncludeItemsByName = true

            });

            // Add search hints based on item name
            hints.AddRange(mediaItems.Where(IncludeInSearch).Select(item =>
            {
                var index = GetIndex(item.Name, searchTerm, terms);

                return new Tuple<BaseItem, string, int>(item, index.Item1, index.Item2);
            }));

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
                if (episode.IsMissingEpisode)
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

            input = input.RemoveDiacritics();

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
