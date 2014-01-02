using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
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
    public class LuceneSearchEngine : ILibrarySearchEngine, IDisposable
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger _logger;

        public LuceneSearchEngine(IServerApplicationPaths serverPaths, ILogManager logManager, ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;

            _logger = logManager.GetLogger("Lucene");
        }

        /// <summary>
        /// Searches items and returns them in order of relevance.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException">searchTerm</exception>
        public IEnumerable<BaseItem> Search(IEnumerable<BaseItem> items, string searchTerm)
        {
            return items;
        }

        public void Dispose()
        {
        }

        /// <summary>
        /// Gets the search hints.
        /// </summary>
        /// <param name="inputItems">The input items.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>IEnumerable{SearchHintResult}.</returns>
        /// <exception cref="System.ArgumentNullException">searchTerm</exception>
        public Task<IEnumerable<SearchHintInfo>> GetSearchHints(IEnumerable<BaseItem> inputItems, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                throw new ArgumentNullException("searchTerm");
            }

            var terms = GetWords(searchTerm);

            var hints = new List<Tuple<BaseItem, string, int>>();

            var items = inputItems.Where(i => !(i is MusicArtist)).ToList();

            // Add search hints based on item name
            hints.AddRange(items.Where(i => !string.IsNullOrEmpty(i.Name)).Select(item =>
            {
                var index = GetIndex(item.Name, searchTerm, terms);

                return new Tuple<BaseItem, string, int>(item, index.Item1, index.Item2);
            }));

            // Find artists
            var artists = _libraryManager.GetAllArtists(items)
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

            // Find genres, from non-audio items
            var genres = items.Where(i => !(i is IHasMusicGenres) && !(i is Game))
                .SelectMany(i => i.Genres)
                .Where(i => !string.IsNullOrEmpty(i))
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
                .Where(i => !string.IsNullOrEmpty(i))
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
                .Where(i => !string.IsNullOrEmpty(i))
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

            // Find studios
            var studios = items.SelectMany(i => i.Studios)
                .Where(i => !string.IsNullOrEmpty(i))
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

            // Find persons
            var persons = items.SelectMany(i => i.People)
                .Select(i => i.Name)
                .Where(i => !string.IsNullOrEmpty(i))
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

            var returnValue = hints.Where(i => i.Item3 >= 0).OrderBy(i => i.Item3).Select(i => new SearchHintInfo
            {
                Item = i.Item1,
                MatchedTerm = i.Item2
            });

            return Task.FromResult(returnValue);
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
            if (string.IsNullOrEmpty(input))
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
            return term.Split().Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        }
    }
}
