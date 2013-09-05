using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MoreLinq;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Folder
    /// </summary>
    public class Folder : BaseItem
    {
        public Folder()
        {
            LinkedChildren = new List<LinkedChild>();
        }

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

        public virtual List<LinkedChild> LinkedChildren { get; set; }

        protected virtual bool SupportsShortcutChildren
        {
            get { return true; }
        }

        /// <summary>
        /// Adds the child.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException">Unable to add  + item.Name</exception>
        public async Task AddChild(BaseItem item, CancellationToken cancellationToken)
        {
            item.Parent = this;

            if (item.Id == Guid.Empty)
            {
                item.Id = item.Path.GetMBId(item.GetType());
            }

            if (item.DateCreated == DateTime.MinValue)
            {
                item.DateCreated = DateTime.UtcNow;
            }
            if (item.DateModified == DateTime.MinValue)
            {
                item.DateModified = DateTime.UtcNow;
            }

            if (!_children.TryAdd(item.Id, item))
            {
                throw new InvalidOperationException("Unable to add " + item.Name);
            }

            await LibraryManager.CreateItem(item, cancellationToken).ConfigureAwait(false);

            await ItemRepository.SaveChildren(Id, _children.Values.ToList().Select(i => i.Id), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Never want folders to be blocked by "BlockNotRated"
        /// </summary>
        [IgnoreDataMember]
        public override string OfficialRatingForComparison
        {
            get
            {
                if (this is Series)
                {
                    return base.OfficialRatingForComparison;
                }

                return !string.IsNullOrEmpty(base.OfficialRatingForComparison) ? base.OfficialRatingForComparison : "None";
            }
        }

        /// <summary>
        /// Removes the child.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.InvalidOperationException">Unable to remove  + item.Name</exception>
        public Task RemoveChild(BaseItem item, CancellationToken cancellationToken)
        {
            BaseItem removed;

            if (!_children.TryRemove(item.Id, out removed))
            {
                throw new InvalidOperationException("Unable to remove " + item.Name);
            }

            item.Parent = null;

            LibraryManager.ReportItemRemoved(item);

            return ItemRepository.SaveChildren(Id, _children.Values.ToList().Select(i => i.Id), cancellationToken);
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
                //{LocalizedStrings.Instance.GetString("OfficialRatingDispPref"), null},
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
            return GetIndexByPerson(user, new List<string> { PersonType.Actor, PersonType.GuestStar }, true, LocalizedStrings.Instance.GetString("PerformerDispPref"));
        }

        /// <summary>
        /// Gets the index by director.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected IEnumerable<BaseItem> GetIndexByDirector(User user)
        {
            return GetIndexByPerson(user, new List<string> { PersonType.Director }, false, LocalizedStrings.Instance.GetString("DirectorDispPref"));
        }

        /// <summary>
        /// Gets the index by person.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="personTypes">The person types we should match on</param>
        /// <param name="includeAudio">if set to <c>true</c> [include audio].</param>
        /// <param name="indexName">Name of the index.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        private IEnumerable<BaseItem> GetIndexByPerson(User user, List<string> personTypes, bool includeAudio, string indexName)
        {
            // Even though this implementation means multiple iterations over the target list - it allows us to defer
            // the retrieval of the individual children for each index value until they are requested.
            using (new Profiler(indexName + " Index Build for " + Name, Logger))
            {
                // Put this in a local variable to avoid an implicitly captured closure
                var currentIndexName = indexName;

                var us = this;
                var recursiveChildren = GetRecursiveChildren(user).Where(i => i.IncludeInIndex).ToList();

                // Get the candidates, but handle audio separately
                var candidates = recursiveChildren.Where(i => i.AllPeople != null && !(i is Audio.Audio)).ToList();

                var indexFolders = candidates.AsParallel().SelectMany(i => i.AllPeople.Where(p => personTypes.Contains(p.Type))
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
                                        ), currentIndexName)).AsEnumerable();

                if (includeAudio)
                {
                    var songs = recursiveChildren.OfType<Audio.Audio>().ToList();

                    indexFolders = songs.SelectMany(i => i.Artists)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(i =>
                    {
                        try
                        {
                            return LibraryManager.GetArtist(i).Result;
                        }
                        catch (IOException ex)
                        {
                            Logger.ErrorException("Error getting artist {0}", ex, i);
                            return null;
                        }
                        catch (AggregateException ex)
                        {
                            Logger.ErrorException("Error getting artist {0}", ex, i);
                            return null;
                        }
                    })
                    .Where(i => i != null)
                    .Select(a => new IndexFolder(us, a,
                                        songs.Where(i => i.Artists.Contains(a.Name, StringComparer.OrdinalIgnoreCase)
                                        ), currentIndexName)).Concat(indexFolders);
                }

                return indexFolders;
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

                var candidates = GetRecursiveChildren(user).Where(i => i.IncludeInIndex).ToList();

                return candidates.AsParallel().SelectMany(i => i.AllStudios)
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
                    .Select(ndx => new IndexFolder(this, ndx, candidates.Where(i => i.AllStudios.Any(s => s.Equals(ndx.Name, StringComparison.OrdinalIgnoreCase))), indexName));
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
                var candidates = GetRecursiveChildren(user).Where(i => i.IncludeInIndex).ToList();

                return candidates.AsParallel().SelectMany(i => i.AllGenres)
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
                    .Select(genre => new IndexFolder(this, genre, candidates.Where(i => i.AllGenres.Any(g => g.Equals(genre.Name, StringComparison.OrdinalIgnoreCase))), indexName)
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
                var candidates = GetRecursiveChildren(user).Where(i => i.IncludeInIndex && i.ProductionYear.HasValue).ToList();

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
        private ConcurrentDictionary<Guid, BaseItem> _children;
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
        protected virtual ConcurrentDictionary<Guid, BaseItem> ActualChildren
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
        public IEnumerable<BaseItem> Children
        {
            get
            {
                return ActualChildren.Values.ToList();
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
        protected virtual ConcurrentDictionary<Guid, BaseItem> LoadChildren()
        {
            //just load our children from the repo - the library will be validated and maintained in other processes
            return new ConcurrentDictionary<Guid, BaseItem>(GetCachedChildren().ToDictionary(i => i.Id));
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
        /// <param name="forceRefreshMetadata">if set to <c>true</c> [force refresh metadata].</param>
        /// <returns>Task.</returns>
        public async Task ValidateChildren(IProgress<double> progress, CancellationToken cancellationToken, bool? recursive = null, bool forceRefreshMetadata = false)
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

                await ValidateChildrenInternal(progress, linkedCancellationTokenSource.Token, recursive, forceRefreshMetadata).ConfigureAwait(false);
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
        /// <param name="forceRefreshMetadata">if set to <c>true</c> [force refresh metadata].</param>
        /// <returns>Task.</returns>
        protected async virtual Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool? recursive = null, bool forceRefreshMetadata = false)
        {
            var locationType = LocationType;

            // Nothing to do here
            if (locationType == LocationType.Remote || locationType == LocationType.Virtual)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            IEnumerable<BaseItem> nonCachedChildren;

            try
            {
                nonCachedChildren = GetNonCachedChildren();
            }
            catch (IOException ex)
            {
                nonCachedChildren = new BaseItem[] { };

                Logger.ErrorException("Error getting file system entries for {0}", ex, Path);
            }

            if (nonCachedChildren == null) return; //nothing to validate

            progress.Report(5);

            //build a dictionary of the current children we have now by Id so we can compare quickly and easily
            var currentChildren = ActualChildren;

            //create a list for our validated children
            var validChildren = new ConcurrentBag<Tuple<BaseItem, bool>>();
            var newItems = new ConcurrentBag<BaseItem>();

            cancellationToken.ThrowIfCancellationRequested();

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 20
            };

            Parallel.ForEach(nonCachedChildren, options, child =>
            {
                BaseItem currentChild;

                if (currentChildren.TryGetValue(child.Id, out currentChild))
                {
                    currentChild.ResolveArgs = child.ResolveArgs;

                    //existing item - check if it has changed
                    if (currentChild.HasChanged(child))
                    {
                        EntityResolutionHelper.EnsureDates(currentChild, child.ResolveArgs, false);

                        validChildren.Add(new Tuple<BaseItem, bool>(currentChild, true));
                    }
                    else
                    {
                        validChildren.Add(new Tuple<BaseItem, bool>(currentChild, false));
                    }

                    currentChild.IsOffline = false;
                }
                else
                {
                    //brand new item - needs to be added
                    newItems.Add(child);

                    validChildren.Add(new Tuple<BaseItem, bool>(child, true));
                }
            });

            // If any items were added or removed....
            if (!newItems.IsEmpty || currentChildren.Count != validChildren.Count)
            {
                var newChildren = validChildren.Select(c => c.Item1).ToList();

                //that's all the new and changed ones - now see if there are any that are missing
                var itemsRemoved = currentChildren.Values.Except(newChildren).ToList();

                foreach (var item in itemsRemoved)
                {
                    if (IsRootPathAvailable(item.Path))
                    {
                        item.IsOffline = false;

                        BaseItem removed;

                        if (!_children.TryRemove(item.Id, out removed))
                        {
                            Logger.Error("Failed to remove {0}", item.Name);
                        }
                        else
                        {
                            LibraryManager.ReportItemRemoved(item);
                        }
                    }
                    else
                    {
                        item.IsOffline = true;

                        validChildren.Add(new Tuple<BaseItem, bool>(item, false));
                    }
                }

                await LibraryManager.CreateItems(newItems, cancellationToken).ConfigureAwait(false);

                foreach (var item in newItems)
                {
                    if (!_children.TryAdd(item.Id, item))
                    {
                        Logger.Error("Failed to add {0}", item.Name);
                    }
                    else
                    {
                        Logger.Debug("** " + item.Name + " Added to library.");
                    }
                }

                await ItemRepository.SaveChildren(Id, _children.Values.ToList().Select(i => i.Id), cancellationToken).ConfigureAwait(false);

                //force the indexes to rebuild next time
                IndexCache.Clear();
            }

            progress.Report(10);

            cancellationToken.ThrowIfCancellationRequested();

            await RefreshChildren(validChildren, progress, cancellationToken, recursive, forceRefreshMetadata).ConfigureAwait(false);

            progress.Report(100);
        }

        /// <summary>
        /// Refreshes the children.
        /// </summary>
        /// <param name="children">The children.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <param name="forceRefreshMetadata">if set to <c>true</c> [force refresh metadata].</param>
        /// <returns>Task.</returns>
        private async Task RefreshChildren(IEnumerable<Tuple<BaseItem, bool>> children, IProgress<double> progress, CancellationToken cancellationToken, bool? recursive, bool forceRefreshMetadata = false)
        {
            var list = children.ToList();

            var percentages = new Dictionary<Guid, double>();

            var tasks = new List<Task>();

            foreach (var tuple in list)
            {
                if (tasks.Count > 8)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }

                Tuple<BaseItem, bool> currentTuple = tuple;

                tasks.Add(Task.Run(async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var child = currentTuple.Item1;

                    //refresh it
                    await child.RefreshMetadata(cancellationToken, forceSave: currentTuple.Item2, forceRefresh: forceRefreshMetadata, resetResolveArgs: false).ConfigureAwait(false);

                    // Refresh children if a folder and the item changed or recursive is set to true
                    var refreshChildren = child.IsFolder && (currentTuple.Item2 || (recursive.HasValue && recursive.Value));

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
                            lock (percentages)
                            {
                                percentages[child.Id] = p / 100;

                                var percent = percentages.Values.Sum();
                                percent /= list.Count;

                                progress.Report((90 * percent) + 10);
                            }
                        });

                        await ((Folder)child).ValidateChildren(innerProgress, cancellationToken, recursive, forceRefreshMetadata).ConfigureAwait(false);

                        // Some folder providers are unable to refresh until children have been refreshed.
                        await child.RefreshMetadata(cancellationToken, resetResolveArgs: false).ConfigureAwait(false);
                    }
                    else
                    {
                        lock (percentages)
                        {
                            percentages[child.Id] = 1;

                            var percent = percentages.Values.Sum();
                            percent /= list.Count;

                            progress.Report((90 * percent) + 10);
                        }
                    }
                }));
            }

            cancellationToken.ThrowIfCancellationRequested();

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <summary>
        /// Determines if a path's root is available or not
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private bool IsRootPathAvailable(string path)
        {
            if (File.Exists(path))
            {
                return true;
            }

            // Depending on whether the path is local or unc, it may return either null or '\' at the top
            while (!string.IsNullOrEmpty(path) && path.Length > 1)
            {
                if (Directory.Exists(path))
                {
                    return true;
                }

                path = System.IO.Path.GetDirectoryName(path);
            }

            return false;
        }

        /// <summary>
        /// Get the children of this folder from the actual file system
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected virtual IEnumerable<BaseItem> GetNonCachedChildren()
        {

            if (ResolveArgs == null || ResolveArgs.FileSystemDictionary == null)
            {
                Logger.Error("Null for {0}", Path);
            }

            return LibraryManager.ResolvePaths<BaseItem>(ResolveArgs.FileSystemChildren, this);
        }

        /// <summary>
        /// Get our children from the repo - stubbed for now
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected IEnumerable<BaseItem> GetCachedChildren()
        {
            return ItemRepository.GetChildren(Id).Select(RetrieveChild).Where(i => i != null);
        }

        /// <summary>
        /// Retrieves the child.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns>BaseItem.</returns>
        private BaseItem RetrieveChild(Guid child)
        {
            var item = LibraryManager.RetrieveItem(child);

            if (item != null)
            {
                if (item is IByReferenceItem)
                {
                    return LibraryManager.GetOrAddByReferenceItem(item);
                }

                item.Parent = this;
            }

            return item;
        }

        /// <summary>
        /// Gets allowed children of an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="includeLinkedChildren">if set to <c>true</c> [include linked children].</param>
        /// <param name="indexBy">The index by.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public virtual IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren, string indexBy = null)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            //the true root should return our users root folder children
            if (IsPhysicalRoot) return user.RootFolder.GetChildren(user, includeLinkedChildren, indexBy);

            IEnumerable<BaseItem> result = null;

            if (!string.IsNullOrEmpty(indexBy))
            {
                result = GetIndexedChildren(user, indexBy);
            }

            if (result != null)
            {
                return result;
            }

            var children = Children;

            if (includeLinkedChildren)
            {
                children = children.Concat(GetLinkedChildren());
            }

            // If indexed is false or the indexing function is null
            return children.Where(c => c.IsVisible(user));
        }

        /// <summary>
        /// Gets allowed recursive children of an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="includeLinkedChildren">if set to <c>true</c> [include linked children].</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEnumerable<BaseItem> GetRecursiveChildren(User user, bool includeLinkedChildren = true)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            var children = GetRecursiveChildrenInternal(user, includeLinkedChildren);

            if (includeLinkedChildren)
            {
                children = children.DistinctBy(i => i.Id);
            }

            return children;
        }

        /// <summary>
        /// Gets allowed recursive children of an item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="includeLinkedChildren">if set to <c>true</c> [include linked children].</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        private IEnumerable<BaseItem> GetRecursiveChildrenInternal(User user, bool includeLinkedChildren)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            foreach (var item in GetChildren(user, includeLinkedChildren))
            {
                yield return item;

                var subFolder = item as Folder;

                if (subFolder != null)
                {
                    foreach (var subitem in subFolder.GetRecursiveChildrenInternal(user, includeLinkedChildren))
                    {
                        yield return subitem;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the linked children.
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        public IEnumerable<BaseItem> GetLinkedChildren()
        {
            return LinkedChildren
                .Select(GetLinkedChild)
                .Where(i => i != null);
        }

        /// <summary>
        /// Gets the linked child.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>BaseItem.</returns>
        private BaseItem GetLinkedChild(LinkedChild info)
        {
            var item = LibraryManager.RootFolder.FindByPath(info.Path);

            if (item == null)
            {
                Logger.Warn("Unable to find linked item at {0}", info.Path);
            }

            return item;
        }

        public override async Task<bool> RefreshMetadata(CancellationToken cancellationToken, bool forceSave = false, bool forceRefresh = false, bool allowSlowProviders = true, bool resetResolveArgs = true)
        {
            var changed = await base.RefreshMetadata(cancellationToken, forceSave, forceRefresh, allowSlowProviders, resetResolveArgs).ConfigureAwait(false);

            return changed || (SupportsShortcutChildren && LocationType == LocationType.FileSystem && RefreshLinkedChildren());
        }

        /// <summary>
        /// Refreshes the linked children.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool RefreshLinkedChildren()
        {
            ItemResolveArgs resolveArgs;

            try
            {
                resolveArgs = ResolveArgs;

                if (!resolveArgs.IsDirectory)
                {
                    return false;
                }
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error getting ResolveArgs for {0}", ex, Path);
                return false;
            }

            var currentManualLinks = LinkedChildren.Where(i => i.Type == LinkedChildType.Manual).ToList();
            var currentShortcutLinks = LinkedChildren.Where(i => i.Type == LinkedChildType.Shortcut).ToList();

            var newShortcutLinks = resolveArgs.FileSystemChildren
                .Where(i => (i.Attributes & FileAttributes.Directory) != FileAttributes.Directory && FileSystem.IsShortcut(i.FullName))
                .Select(i =>
                {
                    try
                    {
                        Logger.Debug("Found shortcut at {0}", i.FullName);
                        
                        return new LinkedChild
                        {
                            Path = FileSystem.ResolveShortcut(i.FullName),
                            Type = LinkedChildType.Shortcut
                        };
                    }
                    catch (IOException ex)
                    {
                        Logger.ErrorException("Error resolving shortcut {0}", ex, i.FullName);
                        return null;
                    }
                })
                .Where(i => i != null)
                .ToList();

            if (!newShortcutLinks.SequenceEqual(currentShortcutLinks))
            {
                Logger.Info("Shortcut links have changed for {0}", Path);

                newShortcutLinks.AddRange(currentManualLinks);
                LinkedChildren = newShortcutLinks;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Folders need to validate and refresh
        /// </summary>
        /// <returns>Task.</returns>
        public override async Task ChangedExternally()
        {
            await base.ChangedExternally().ConfigureAwait(false);

            var progress = new Progress<double>();

            await ValidateChildren(progress, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Marks the item as either played or unplayed
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="wasPlayed">if set to <c>true</c> [was played].</param>
        /// <param name="userManager">The user manager.</param>
        /// <returns>Task.</returns>
        public override async Task SetPlayedStatus(User user, bool wasPlayed, IUserDataRepository userManager)
        {
            // Sweep through recursively and update status
            var tasks = GetRecursiveChildren(user, true).Where(i => !i.IsFolder).Select(c => c.SetPlayedStatus(user, wasPlayed, userManager));

            await Task.WhenAll(tasks).ConfigureAwait(false);
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
