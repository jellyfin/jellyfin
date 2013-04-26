using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
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

            //string luceneDbPath = serverPaths.DataPath + "\\SearchIndexDB";
            //if (!System.IO.Directory.Exists(luceneDbPath))
            //    System.IO.Directory.CreateDirectory(luceneDbPath);
            //else if(File.Exists(luceneDbPath + "\\write.lock"))
            //        File.Delete(luceneDbPath + "\\write.lock");

            //LuceneSearch.Init(luceneDbPath, _logger);

            //BaseItem.LibraryManager.LibraryChanged += LibraryChanged;
        }

        public void LibraryChanged(object source, ChildrenChangedEventArgs changeInformation)
        {
            Task.Run(() =>
            {
                if (changeInformation.ItemsAdded.Count + changeInformation.ItemsUpdated.Count > 0)
                {
                    LuceneSearch.AddUpdateLuceneIndex(changeInformation.ItemsAdded.Concat(changeInformation.ItemsUpdated));
                }

                if (changeInformation.ItemsRemoved.Count > 0)
                {
                    LuceneSearch.RemoveFromLuceneIndex(changeInformation.ItemsRemoved);
                }
            });
        }

        public void AddItemsToIndex(IEnumerable<BaseItem> items)
        {
            LuceneSearch.AddUpdateLuceneIndex(items);
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
            if (string.IsNullOrEmpty(searchTerm))
            {
                throw new ArgumentNullException("searchTerm");
            }

            var hits = LuceneSearch.Search(searchTerm, items.Count());

            //return hits;
            return hits.Where(searchHit => items.Any(p => p.Id == searchHit.Id));
        }

        public void Dispose()
        {
            //BaseItem.LibraryManager.LibraryChanged -= LibraryChanged;

            //LuceneSearch.CloseAll();
        }

        /// <summary>
        /// Gets the search hints.
        /// </summary>
        /// <param name="inputItems">The input items.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>IEnumerable{SearchHintResult}.</returns>
        /// <exception cref="System.ArgumentNullException">searchTerm</exception>
        public async Task<IEnumerable<BaseItem>> GetSearchHints(IEnumerable<BaseItem> inputItems, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                throw new ArgumentNullException("searchTerm");
            }

            var hints = new List<Tuple<BaseItem, int>>();

            var items = inputItems.Where(i => !(i is MusicArtist)).ToList();

            foreach (var item in items)
            {
                var index = IndexOf(item.Name, searchTerm);

                if (index != -1)
                {
                    hints.Add(new Tuple<BaseItem, int>(item, index));
                }
            }

            // Find artists
            var artists = items.OfType<Audio>()
                .SelectMany(i => new[] { i.Artist, i.AlbumArtist })
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var item in artists)
            {
                var index = IndexOf(item, searchTerm);

                if (index != -1)
                {
                    var artist = await _libraryManager.GetArtist(item).ConfigureAwait(false);

                    hints.Add(new Tuple<BaseItem, int>(artist, index));
                }
            }
            
            // Find genres
            var genres = items.SelectMany(i => i.Genres)
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var item in genres)
            {
                var index = IndexOf(item, searchTerm);

                if (index != -1)
                {
                    var genre = await _libraryManager.GetGenre(item).ConfigureAwait(false);

                    hints.Add(new Tuple<BaseItem, int>(genre, index));
                }
            }

            // Find studios
            var studios = items.SelectMany(i => i.Studios)
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var item in studios)
            {
                var index = IndexOf(item, searchTerm);

                if (index != -1)
                {
                    var studio = await _libraryManager.GetStudio(item).ConfigureAwait(false);

                    hints.Add(new Tuple<BaseItem, int>(studio, index));
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
                var index = IndexOf(item, searchTerm);

                if (index != -1)
                {
                    var person = await _libraryManager.GetPerson(item).ConfigureAwait(false);

                    hints.Add(new Tuple<BaseItem, int>(person, index));
                }
            }

            return hints.OrderBy(i => i.Item2).Select(i => i.Item1);
        }

        /// <summary>
        /// Gets the words.
        /// </summary>
        /// <param name="term">The term.</param>
        /// <returns>System.String[][].</returns>
        private string[] GetWords(string term)
        {
            // TODO: Improve this to be more accurate and respect culture
            var words = term.Split(' ');

            return words;
        }

        /// <summary>
        /// Indexes the of.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="term">The term.</param>
        /// <returns>System.Int32.</returns>
        private int IndexOf(string input, string term)
        {
            var index = 0;

            foreach (var word in GetWords(input))
            {
                if (word.IndexOf(term, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return index;
                }

                index++;
            }
            return -1;
        }
    }

    public static class LuceneSearch
    {
        private static ILogger logger;

        private static string path;
        private static object lockOb = new object();

        private static FSDirectory _directory;
        private static FSDirectory directory
        {
            get
            {
                if (_directory == null)
                {
                    logger.Info("Opening new Directory: " + path);
                    _directory = FSDirectory.Open(path);
                }
                return _directory;
            }
            set
            {
                _directory = value;
            }
        }

        private static IndexWriter _writer;
        private static IndexWriter writer
        {
            get
            {
                if (_writer == null)
                {
                    logger.Info("Opening new IndexWriter");
                    _writer = new IndexWriter(directory, analyzer, IndexWriter.MaxFieldLength.UNLIMITED);
                }
                return _writer;
            }
            set
            {
                _writer = value;
            }
        }

        private static Dictionary<string, float> bonusTerms;

        public static void Init(string path, ILogger logger)
        {
            logger.Info("Lucene: Init");

            bonusTerms = new Dictionary<string, float>();
            bonusTerms.Add("Name", 2);
            bonusTerms.Add("Overview", 1);

            // Optimize the DB on initialization
            // TODO: Test whether this has..
            //      Any effect what-so-ever (apart from initializing the indexwriter on the mainthread context, which makes things a whole lot easier)
            //      Costs too much time
            //      Is heavy on the CPU / Memory

            LuceneSearch.logger = logger;
            LuceneSearch.path = path;

            writer.Optimize();
        }

        private static StandardAnalyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

        private static Searcher searcher = null;

        private static Document createDocument(BaseItem data)
        {
            Document doc = new Document();

            doc.Add(new Field("Id", data.Id.ToString(), Field.Store.YES, Field.Index.NO));
            doc.Add(new Field("Name", data.Name, Field.Store.YES, Field.Index.ANALYZED) { Boost = 2 });
            doc.Add(new Field("Overview", data.Overview != null ? data.Overview : "", Field.Store.YES, Field.Index.ANALYZED));

            return doc;
        }

        private static void Create(BaseItem item)
        {
            lock (lockOb)
            {
                try
                {
                    if (searcher != null)
                    {
                        try
                        {
                            searcher.Dispose();
                        }
                        catch (Exception e)
                        {
                            logger.ErrorException("Error in Lucene while creating index (disposing alive searcher)", e, item);
                        }

                        searcher = null;
                    }

                    _removeFromLuceneIndex(item);
                    _addToLuceneIndex(item);
                }
                catch (Exception e)
                {
                    logger.ErrorException("Error in Lucene while creating index", e, item);
                }
            }
        }

        private static void _addToLuceneIndex(BaseItem data)
        {
            // Prevent double entries
            var doc = createDocument(data);

            writer.AddDocument(doc);
        }

        private static void _removeFromLuceneIndex(BaseItem data)
        {
            var query = new TermQuery(new Term("Id", data.Id.ToString()));
            writer.DeleteDocuments(query);
        }

        public static void AddUpdateLuceneIndex(IEnumerable<BaseItem> items)
        {
            foreach (var item in items)
            {
                logger.Info("Adding/Updating BaseItem " + item.Name + "(" + item.Id.ToString() + ") to/on Lucene Index");
                Create(item);
            }

            writer.Commit();
            writer.Flush(true, true, true);
        }

        public static void RemoveFromLuceneIndex(IEnumerable<BaseItem> items)
        {
            foreach (var item in items)
            {
                logger.Info("Removing BaseItem " + item.Name + "(" + item.Id.ToString() + ") from Lucene Index");
                _removeFromLuceneIndex(item);
            }

            writer.Commit();
            writer.Flush(true, true, true);
        }

        public static IEnumerable<BaseItem> Search(string searchQuery, int maxHits)
        {
            var results = new List<BaseItem>();

            lock (lockOb)
            {
                try
                {
                    if (searcher == null)
                    {
                        searcher = new IndexSearcher(directory, true);
                    }

                    BooleanQuery finalQuery = new BooleanQuery();

                    MultiFieldQueryParser parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, new string[] { "Name", "Overview" }, analyzer, bonusTerms);

                    string[] terms = searchQuery.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string term in terms)
                        finalQuery.Add(parser.Parse(term.Replace("~", "") + "~0.75"), Occur.SHOULD);
                    foreach (string term in terms)
                        finalQuery.Add(parser.Parse(term.Replace("*", "") + "*"), Occur.SHOULD);

                    logger.Debug("Querying Lucene with query:   " + finalQuery.ToString());

                    long start = DateTime.Now.Ticks;
                    var searchResult = searcher.Search(finalQuery, maxHits);
                    foreach (var searchHit in searchResult.ScoreDocs)
                    {
                        Document hit = searcher.Doc(searchHit.Doc);
                        results.Add(BaseItem.LibraryManager.GetItemById(Guid.Parse(hit.Get("Id"))));
                    }
                    long total = DateTime.Now.Ticks - start;
                    float msTotal = (float)total / TimeSpan.TicksPerMillisecond;
                    logger.Debug(searchResult.ScoreDocs.Length + " result" + (searchResult.ScoreDocs.Length == 1 ? "" : "s") + " in " + msTotal + " ms.");
                }
                catch (Exception e)
                {
                    logger.ErrorException("Error while searching Lucene index", e);
                }
            }

            return results;
        }

        public static void CloseAll()
        {
            logger.Debug("Lucene: CloseAll");
            if (writer != null)
            {
                logger.Debug("Lucene: CloseAll - Writer is alive");
                writer.Flush(true, true, true);
                writer.Commit();
                writer.WaitForMerges();
                writer.Dispose();
                writer = null;
            }
            if (analyzer != null)
            {
                logger.Debug("Lucene: CloseAll - Analyzer is alive");
                analyzer.Close();
                analyzer.Dispose();
                analyzer = null;
            }
            if (searcher != null)
            {
                logger.Debug("Lucene: CloseAll - Searcher is alive");
                searcher.Dispose();
                searcher = null;
            }
            if (directory != null)
            {
                logger.Debug("Lucene: CloseAll - Directory is alive");
                directory.Dispose();
                directory = null;
            }
        }
    }
}
