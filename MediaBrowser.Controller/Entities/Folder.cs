using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Folder
    /// </summary>
    public class Folder : BaseItem
    {
        /// <summary>
        /// Gets a value indicating whether this instance is folder.
        /// </summary>
        /// <value><c>true</c> if this instance is folder; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IsFolder
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is physical root.
        /// </summary>
        /// <value><c>true</c> if this instance is physical root; otherwise, <c>false</c>.</value>
        public bool IsPhysicalRoot { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is root.
        /// </summary>
        /// <value><c>true</c> if this instance is root; otherwise, <c>false</c>.</value>
        public bool IsRoot { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is virtual folder.
        /// </summary>
        /// <value><c>true</c> if this instance is virtual folder; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public virtual bool IsVirtualFolder
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Return the id that should be used to key display prefs for this item.
        /// Default is based on the type for everything except actual generic folders.
        /// </summary>
        /// <value>The display prefs id.</value>
        [IgnoreDataMember]
        public virtual Guid DisplayPreferencesId
        {
            get
            {
                var thisType = GetType();
                return thisType == typeof(Folder) ? Id : thisType.FullName.GetMD5();
            }
        }

        /// <summary>
        /// The _display prefs
        /// </summary>
        private IEnumerable<DisplayPreferences> _displayPreferences;
        /// <summary>
        /// The _display prefs initialized
        /// </summary>
        private bool _displayPreferencesInitialized;
        /// <summary>
        /// The _display prefs sync lock
        /// </summary>
        private object _displayPreferencesSyncLock = new object();
        /// <summary>
        /// Gets the display prefs.
        /// </summary>
        /// <value>The display prefs.</value>
        [IgnoreDataMember]
        public IEnumerable<DisplayPreferences> DisplayPreferences
        {
            get
            {
                // Call ToList to exhaust the stream because we'll be iterating over this multiple times
                LazyInitializer.EnsureInitialized(ref _displayPreferences, ref _displayPreferencesInitialized, ref _displayPreferencesSyncLock, () => Kernel.Instance.DisplayPreferencesRepository.RetrieveDisplayPreferences(this).ToList());
                return _displayPreferences;
            }
            private set
            {
                _displayPreferences = value;

                if (value == null)
                {
                    _displayPreferencesInitialized = false;
                }
            }
        }

        /// <summary>
        /// Gets the display prefs.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="createIfNull">if set to <c>true</c> [create if null].</param>
        /// <returns>DisplayPreferences.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public DisplayPreferences GetDisplayPreferences(User user, bool createIfNull)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            if (DisplayPreferences == null)
            {
                if (!createIfNull)
                {
                    return null;
                }

                AddOrUpdateDisplayPreferences(user, new DisplayPreferences { UserId = user.Id });
            }

            var data = DisplayPreferences.FirstOrDefault(u => u.UserId == user.Id);

            if (data == null && createIfNull)
            {
                data = new DisplayPreferences { UserId = user.Id };
                AddOrUpdateDisplayPreferences(user, data);
            }

            return data;
        }

        /// <summary>
        /// Adds the or update display prefs.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="data">The data.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void AddOrUpdateDisplayPreferences(User user, DisplayPreferences data)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            if (data == null)
            {
                throw new ArgumentNullException();
            }

            data.UserId = user.Id;

            if (DisplayPreferences == null)
            {
                DisplayPreferences = new[] { data };
            }
            else
            {
                var list = DisplayPreferences.Where(u => u.UserId != user.Id).ToList();
                list.Add(data);
                DisplayPreferences = list;
            }
        }

        #region Indexing

        /// <summary>
        /// The _index by options
        /// </summary>
        private Dictionary<string, Func<User, IEnumerable<BaseItem>>> _indexByOptions;
        /// <summary>
        /// Dictionary of index options - consists of a display value and an indexing function
        /// which takes User as a parameter and returns an IEnum of BaseItem
        /// </summary>
        /// <value>The index by options.</value>
        [IgnoreDataMember]
        public Dictionary<string, Func<User, IEnumerable<BaseItem>>> IndexByOptions
        {
            get { return _indexByOptions ?? (_indexByOptions = GetIndexByOptions()); }
        }

        /// <summary>
        /// Returns the valid set of index by options for this folder type.
        /// Override or extend to modify.
        /// </summary>
        /// <returns>Dictionary{System.StringFunc{UserIEnumerable{BaseItem}}}.</returns>
        protected virtual Dictionary<string, Func<User, IEnumerable<BaseItem>>> GetIndexByOptions()
        {
            return new Dictionary<string, Func<User, IEnumerable<BaseItem>>> {            
                {LocalizedStrings.Instance.GetString("NoneDispPref"), null}, 
                {LocalizedStrings.Instance.GetString("PerformerDispPref"), GetIndexByPerformer},
                {LocalizedStrings.Instance.GetString("GenreDispPref"), GetIndexByGenre},
                {LocalizedStrings.Instance.GetString("DirectorDispPref"), GetIndexByDirector},
                {LocalizedStrings.Instance.GetString("YearDispPref"), GetIndexByYear},
                {LocalizedStrings.Instance.GetString("OfficialRatingDispPref"), null},
                {LocalizedStrings.Instance.GetString("StudioDispPref"), GetIndexByStudio}
            };

        }

        /// <summary>
        /// Gets the index by actor.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected IEnumerable<BaseItem> GetIndexByPerformer(User user)
        {
            return GetIndexByPerson(user, new List<string> { PersonType.Actor, PersonType.MusicArtist }, LocalizedStrings.Instance.GetString("PerformerDispPref"));
        }

        /// <summary>
        /// Gets the index by director.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected IEnumerable<BaseItem> GetIndexByDirector(User user)
        {
            return GetIndexByPerson(user, new List<string> { PersonType.Director }, LocalizedStrings.Instance.GetString("DirectorDispPref"));
        }

        /// <summary>
        /// Gets the index by person.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="personTypes">The person types we should match on</param>
        /// <param name="indexName">Name of the index.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected IEnumerable<BaseItem> GetIndexByPerson(User user, List<string> personTypes, string indexName)
        {

            // Even though this implementation means multiple iterations over the target list - it allows us to defer
            // the retrieval of the individual children for each index value until they are requested.
            using (new Profiler(indexName + " Index Build for " + Name, Logger))
            {
                // Put this in a local variable to avoid an implicitly captured closure
                var currentIndexName = indexName;

                var us = this;
                var candidates = RecursiveChildren.Where(i => i.IncludeInIndex && i.AllPeople != null).ToList();

                return candidates.AsParallel().SelectMany(i => i.AllPeople.Where(p => personTypes.Contains(p.Type))
                    .Select(a => a.Name))
                    .Distinct()
                    .Select(i =>
                    {
                        try
                        {
                            return LibraryManager.GetPerson(i).Result;
                        }
                        catch (IOException ex)
                        {
                            Logger.ErrorException("Error getting person {0}", ex, i);
                            return null;
                        }
                        catch (AggregateException ex)
                        {
                            Logger.ErrorException("Error getting person {0}", ex, i);
                            return null;
                        }
                    })
                    .Where(i => i != null)
                    .Select(a => new IndexFolder(us, a,
                                        candidates.Where(i => i.AllPeople.Any(p => personTypes.Contains(p.Type) && p.Name.Equals(a.Name, StringComparison.OrdinalIgnoreCase))
                                        ), currentIndexName));

            }
        }

        /// <summary>
        /// Gets the index by studio.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected IEnumerable<BaseItem> GetIndexByStudio(User user)
        {
            // Even though this implementation means multiple iterations over the target list - it allows us to defer
            // the retrieval of the individual children for each index value until they are requested.
            using (new Profiler("Studio Index Build for " + Name, Logger))
            {
                var indexName = LocalizedStrings.Instance.GetString("StudioDispPref");

                var candidates = RecursiveChildren.Where(i => i.IncludeInIndex && i.Studios != null).ToList();

                return candidates.AsParallel().SelectMany(i => i.Studios)
                    .Distinct()
                    .Select(i =>
                    {
                        try
                        {
                            return LibraryManager.GetStudio(i).Result;
                        }
                        catch (IOException ex)
                        {
                            Logger.ErrorException("Error getting studio {0}", ex, i);
                            return null;
                        }
                        catch (AggregateException ex)
                        {
                            Logger.ErrorException("Error getting studio {0}", ex, i);
                            return null;
                        }
                    })
                    .Where(i => i != null)
                    .Select(ndx => new IndexFolder(this, ndx, candidates.Where(i => i.Studios.Any(s => s.Equals(ndx.Name, StringComparison.OrdinalIgnoreCase))), indexName));
            }
        }

        /// <summary>
        /// Gets the index by genre.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected IEnumerable<BaseItem> GetIndexByGenre(User user)
        {
            // Even though this implementation means multiple iterations over the target list - it allows us to defer
            // the retrieval of the individual children for each index value until they are requested.
            using (new Profiler("Genre Index Build for " + Name, Logger))
            {
                var indexName = LocalizedStrings.Instance.GetString("GenreDispPref");

                //we need a copy of this so we don't double-recurse
                var candidates = RecursiveChildren.Where(i => i.IncludeInIndex && i.Genres != null).ToList();

                return candidates.AsParallel().SelectMany(i => i.Genres)
                    .Distinct()
                    .Select(i =>
                        {
                            try
                            {
                                return LibraryManager.GetGenre(i).Result;
                            }
                            catch (IOException ex)
                            {
                                Logger.ErrorException("Error getting genre {0}", ex, i);
                                return null;
                            }
                            catch (AggregateException ex)
                            {
                                Logger.ErrorException("Error getting genre {0}", ex, i);
                                return null;
                            }
                        })
                    .Where(i => i != null)
                    .Select(genre => new IndexFolder(this, genre, candidates.Where(i => i.Genres.Any(g => g.Equals(genre.Name, StringComparison.OrdinalIgnoreCase))), indexName)
                );
            }
        }

        /// <summary>
        /// Gets the index by year.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected IEnumerable<BaseItem> GetIndexByYear(User user)
        {
            // Even though this implementation means multiple iterations over the target list - it allows us to defer
            // the retrieval of the individual children for each index value until they are requested.
            using (new Profiler("Production Year Index Build for " + Name, Logger))
            {
                var indexName = LocalizedStrings.Instance.GetString("YearDispPref");

                //we need a copy of this so we don't double-recurse
                var candidates = RecursiveChildren.Where(i => i.IncludeInIndex && i.ProductionYear.HasValue).ToList();

                return candidates.AsParallel().Select(i => i.ProductionYear.Value)
                    .Distinct()
                    .Select(i =>
                    {
                        try
                        {
                            return LibraryManager.GetYear(i).Result;
                        }
                        catch (IOException ex)
                        {
                            Logger.ErrorException("Error getting year {0}", ex, i);
                            return null;
                        }
                        catch (AggregateException ex)
                        {
                            Logger.ErrorException("Error getting year {0}", ex, i);
                            return null;
                        }
                    })
                    .Where(i => i != null)

                    .Select(ndx => new IndexFolder(this, ndx, candidates.Where(i => i.ProductionYear == int.Parse(ndx.Name)), indexName));

            }
        }

        /// <summary>
        /// Returns the indexed children for this user from the cache. Caches them if not already there.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="indexBy">The index by.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> GetIndexedChildren(User user, string indexBy)
        {
            List<BaseItem> result;
            var cacheKey = user.Name + indexBy;
            IndexCache.TryGetValue(cacheKey, out result);

            if (result == null)
            {
                //not cached - cache it
                Func<User, IEnumerable<BaseItem>> indexing;
                IndexByOptions.TryGetValue(indexBy, out indexing);
                result = BuildIndex(indexBy, indexing, user);
            }
            return result;
        }

        /// <summary>
        /// Get the list of indexy by choices for this folder (localized).
        /// </summary>
        /// <value>The index by option strings.</value>
        [IgnoreDataMember]
        public IEnumerable<string> IndexByOptionStrings
        {
            get { return IndexByOptions.Keys; }
        }

        /// <summary>
        /// The index cache
        /// </summary>
        protected ConcurrentDictionary<string, List<BaseItem>> IndexCache = new ConcurrentDictionary<string, List<BaseItem>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Builds the index.
        /// </summary>
        /// <param name="indexKey">The index key.</param>
        /// <param name="indexFunction">The index function.</param>
        /// <param name="user">The user.</param>
        /// <returns>List{BaseItem}.</returns>
        protected virtual List<BaseItem> BuildIndex(string indexKey, Func<User, IEnumerable<BaseItem>> indexFunction, User user)
        {
            return indexFunction != null
                       ? IndexCache[user.Name + indexKey] = indexFunction(user).ToList()
                       : null;
        }

        #endregion

        /// <summary>
        /// The children
        /// </summary>
        private ConcurrentBag<BaseItem> _children;
        /// <summary>
        /// The _children initialized
        /// </summary>
        private bool _childrenInitialized;
        /// <summary>
        /// The _children sync lock
        /// </summary>
        private object _childrenSyncLock = new object();
        /// <summary>
        /// Gets or sets the actual children.
        /// </summary>
        /// <value>The actual children.</value>
        protected virtual ConcurrentBag<BaseItem> ActualChildren
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _children, ref _childrenInitialized, ref _childrenSyncLock, LoadChildren);
                return _children;
            }
            private set
            {
                _children = value;

                if (value == null)
                {
                    _childrenInitialized = false;
                }
            }
        }

        /// <summary>
        /// thread-safe access to the actual children of this folder - without regard to user
        /// </summary>
        /// <value>The children.</value>
        [IgnoreDataMember]
        public ConcurrentBag<BaseItem> Children
        {
            get
            {
                return ActualChildren;
            }
        }

        /// <summary>
        /// thread-safe access to all recursive children of this folder - without regard to user
        /// </summary>
        /// <value>The recursive children.</value>
        [IgnoreDataMember]
        public IEnumerable<BaseItem> RecursiveChildren
        {
            get
            {
                foreach (var item in Children)
                {
                    yield return item;

                    if (item.IsFolder)
                    {
                        var subFolder = (Folder)item;

                        foreach (var subitem in subFolder.RecursiveChildren)
                        {
                            yield return subitem;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Loads our children.  Validation will occur externally.
        /// We want this sychronous.
        /// </summary>
        /// <returns>ConcurrentBag{BaseItem}.</returns>
        protected virtual ConcurrentBag<BaseItem> LoadChildren()
        {
            //just load our children from the repo - the library will be validated and maintained in other processes
            return new ConcurrentBag<BaseItem>(GetCachedChildren());
        }

        /// <summary>
        /// Gets or sets the current validation cancellation token source.
        /// </summary>
        /// <value>The current validation cancellation token source.</value>
        private CancellationTokenSource CurrentValidationCancellationTokenSource { get; set; }

        /// <summary>
        /// Validates that the children of the folder still exist
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>Task.</returns>
        public async Task ValidateChildren(IProgress<double> progress, CancellationToken cancellationToken, bool? recursive = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Cancel the current validation, if any
            if (CurrentValidationCancellationTokenSource != null)
            {
                CurrentValidationCancellationTokenSource.Cancel();
            }

            // Create an inner cancellation token. This can cancel all validations from this level on down,
            // but nothing above this
            var innerCancellationTokenSource = new CancellationTokenSource();

            try
            {
                CurrentValidationCancellationTokenSource = innerCancellationTokenSource;

                var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(innerCancellationTokenSource.Token, cancellationToken);

                await ValidateChildrenInternal(progress, linkedCancellationTokenSource.Token, recursive).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                Logger.Info("ValidateChildren cancelled for " + Name);

                // If the outer cancelletion token in the cause for the cancellation, throw it
                if (cancellationToken.IsCancellationRequested && ex.CancellationToken == cancellationToken)
                {
                    throw;
                }
            }
            finally
            {
                // Null out the token source             
                if (CurrentValidationCancellationTokenSource == innerCancellationTokenSource)
                {
                    CurrentValidationCancellationTokenSource = null;
                }

                innerCancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// Compare our current children (presumably just read from the repo) with the current state of the file system and adjust for any changes
        /// ***Currently does not contain logic to maintain items that are unavailable in the file system***
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>Task.</returns>
        protected async virtual Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool? recursive = null)
        {
            // Nothing to do here
            if (LocationType != LocationType.FileSystem)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var changedArgs = new ChildrenChangedEventArgs(this);

            //get the current valid children from filesystem (or wherever)
            var nonCachedChildren = GetNonCachedChildren();

            if (nonCachedChildren == null) return; //nothing to validate

            progress.Report(5);

            //build a dictionary of the current children we have now by Id so we can compare quickly and easily
            var currentChildren = ActualChildren.ToDictionary(i => i.Id);

            //create a list for our validated children
            var validChildren = new ConcurrentBag<Tuple<BaseItem, bool>>();

            cancellationToken.ThrowIfCancellationRequested();
            
            Parallel.ForEach(nonCachedChildren, child =>
            {
                BaseItem currentChild;

                if (currentChildren.TryGetValue(child.Id, out currentChild))
                {
                    currentChild.ResolveArgs = child.ResolveArgs;

                    //existing item - check if it has changed
                    if (currentChild.HasChanged(child))
                    {
                        EntityResolutionHelper.EnsureDates(currentChild, child.ResolveArgs);

                        changedArgs.AddUpdatedItem(currentChild);
                        validChildren.Add(new Tuple<BaseItem, bool>(currentChild, true));
                    }
                    else
                    {
                        validChildren.Add(new Tuple<BaseItem, bool>(currentChild, false));
                    }
                }
                else
                {
                    //brand new item - needs to be added
                    changedArgs.AddNewItem(child);

                    validChildren.Add(new Tuple<BaseItem, bool>(child, true));
                }
            });

            // If any items were added or removed....
            if (!changedArgs.ItemsAdded.IsEmpty || currentChildren.Count != validChildren.Count)
            {
                var newChildren = validChildren.Select(c => c.Item1).ToList();

                //that's all the new and changed ones - now see if there are any that are missing
                changedArgs.ItemsRemoved = currentChildren.Values.Except(newChildren).ToList();

                foreach (var item in changedArgs.ItemsRemoved)
                {
                    Logger.Info("** " + item.Name + " Removed from library.");
                }

                var childrenReplaced = false;

                if (changedArgs.ItemsRemoved.Count > 0)
                {
                    ActualChildren = new ConcurrentBag<BaseItem>(newChildren);
                    childrenReplaced = true;
                }

                var saveTasks = new List<Task>();

                foreach (var item in changedArgs.ItemsAdded)
                {
                    Logger.Info("** " + item.Name + " Added to library.");

                    if (!childrenReplaced)
                    {
                        _children.Add(item);
                    }

                    saveTasks.Add(Kernel.Instance.ItemRepository.SaveItem(item, CancellationToken.None));
                }

                await Task.WhenAll(saveTasks).ConfigureAwait(false);

                //and save children in repo...
                Logger.Info("*** Saving " + newChildren.Count + " children for " + Name);
                await Kernel.Instance.ItemRepository.SaveChildren(Id, newChildren, CancellationToken.None).ConfigureAwait(false);
            }

            if (changedArgs.HasChange)
            {
                //force the indexes to rebuild next time
                IndexCache.Clear();

                //and fire event
                LibraryManager.ReportLibraryChanged(changedArgs);
            }

            progress.Report(10);

            cancellationToken.ThrowIfCancellationRequested();

            await RefreshChildren(validChildren, progress, cancellationToken, recursive).ConfigureAwait(false);

            progress.Report(100);
        }

        /// <summary>
        /// Refreshes the children.
        /// </summary>
        /// <param name="children">The children.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>Task.</returns>
        private Task RefreshChildren(IEnumerable<Tuple<BaseItem, bool>> children, IProgress<double> progress, CancellationToken cancellationToken, bool? recursive)
        {
            var list = children.ToList();

            var percentages = new ConcurrentDictionary<Guid, double>(list.Select(i => new KeyValuePair<Guid, double>(i.Item1.Id, 0)));

            var tasks = list.Select(tuple => Task.Run(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var child = tuple.Item1;

                //refresh it
                await child.RefreshMetadata(cancellationToken, resetResolveArgs: child.IsFolder).ConfigureAwait(false);

                // Refresh children if a folder and the item changed or recursive is set to true
                var refreshChildren = child.IsFolder && (tuple.Item2 || (recursive.HasValue && recursive.Value));

                if (refreshChildren)
                {
                    // Don't refresh children if explicitly set to false
                    if (recursive.HasValue && recursive.Value == false)
                    {
                        refreshChildren = false;
                    }
                }

                if (refreshChildren)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var innerProgress = new ActionableProgress<double>();

                    innerProgress.RegisterAction(p =>
                    {
                        percentages.TryUpdate(child.Id, p / 100, percentages[child.Id]);

                        var percent = percentages.Values.Sum();
                        percent /= list.Count;

                        progress.Report((90 * percent) + 10);
                    });

                    await ((Folder) child).ValidateChildren(innerProgress, cancellationToken, recursive).ConfigureAwait(false);
                }
                else
                {
                    percentages.TryUpdate(child.Id, 1, percentages[child.Id]);

                    var percent = percentages.Values.Sum();
                    percent /= list.Count;

                    progress.Report((90 * percent) + 10);
                }
            }));

            cancellationToken.ThrowIfCancellationRequested();

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Get the children of this folder from the actual file system
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected virtual IEnumerable<BaseItem> GetNonCachedChildren()
        {
            IEnumerable<WIN32_FIND_DATA> fileSystemChildren;

            try
            {
                fileSystemChildren = ResolveArgs.FileSystemChildren;
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error getting ResolveArgs for {0}", ex, Path);
                return new List<BaseItem> { };
            }

            return LibraryManager.ResolvePaths<BaseItem>(fileSystemChildren, this);
        }

        /// <summary>
        /// Get our children from the repo - stubbed for now
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected virtual IEnumerable<BaseItem> GetCachedChildren()
        {
            return Kernel.Instance.ItemRepository.RetrieveChildren(this).Select(i => i is IByReferenceItem ? LibraryManager.GetOrAddByReferenceItem(i) : i);
        }

        /// <summary>
        /// Gets allowed children of an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="indexBy">The index by.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public virtual IEnumerable<BaseItem> GetChildren(User user, string indexBy = null)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            //the true root should return our users root folder children
            if (IsPhysicalRoot) return user.RootFolder.GetChildren(user, indexBy);

            IEnumerable<BaseItem> result = null;

            if (!string.IsNullOrEmpty(indexBy))
            {
                result = GetIndexedChildren(user, indexBy);
            }

            // If indexed is false or the indexing function is null
            if (result == null)
            {
                result = ActualChildren.Where(c => c.IsVisible(user));
            }

            return result;
        }

        /// <summary>
        /// Gets allowed recursive children of an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEnumerable<BaseItem> GetRecursiveChildren(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            foreach (var item in GetChildren(user))
            {
                yield return item;

                var subFolder = item as Folder;

                if (subFolder != null)
                {
                    foreach (var subitem in subFolder.GetRecursiveChildren(user))
                    {
                        yield return subitem;
                    }
                }
            }
        }

        /// <summary>
        /// Folders need to validate and refresh
        /// </summary>
        /// <returns>Task.</returns>
        public override async Task ChangedExternally()
        {
            await base.ChangedExternally().ConfigureAwait(false);

            var progress = new Progress<double> { };

            await ValidateChildren(progress, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Marks the item as either played or unplayed
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="wasPlayed">if set to <c>true</c> [was played].</param>
        /// <returns>Task.</returns>
        public override async Task SetPlayedStatus(User user, bool wasPlayed, IUserManager userManager)
        {
            await base.SetPlayedStatus(user, wasPlayed, userManager).ConfigureAwait(false);

            // Now sweep through recursively and update status
            var tasks = GetChildren(user).Select(c => c.SetPlayedStatus(user, wasPlayed, userManager));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds an item by ID, recursively
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="user">The user.</param>
        /// <returns>BaseItem.</returns>
        public override BaseItem FindItemById(Guid id, User user)
        {
            var result = base.FindItemById(id, user);

            if (result != null)
            {
                return result;
            }

            var children = user == null ? ActualChildren : GetChildren(user);

            foreach (var child in children)
            {
                result = child.FindItemById(id, user);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds an item by path, recursively
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public BaseItem FindByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }

            try
            {
                if (ResolveArgs.PhysicalLocations.Contains(path, StringComparer.OrdinalIgnoreCase))
                {
                    return this;
                }
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error getting ResolveArgs for {0}", ex, Path);
            }

            //this should be functionally equivilent to what was here since it is IEnum and works on a thread-safe copy
            return RecursiveChildren.FirstOrDefault(i =>
            {
                try
                {
                    return i.ResolveArgs.PhysicalLocations.Contains(path, StringComparer.OrdinalIgnoreCase);
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error getting ResolveArgs for {0}", ex, Path);
                    return false;
                }
            });
        }
    }
}
