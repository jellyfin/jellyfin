using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Common.Logging;
using MediaBrowser.Controller.Resolvers;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    public class Folder : BaseItem
    {
        #region Events
        /// <summary>
        /// Fires whenever a validation routine updates our children.  The added and removed children are properties of the args.
        /// *** Will fire asynchronously. ***
        /// </summary>
        public event EventHandler<ChildrenChangedEventArgs> ChildrenChanged;
        protected void OnChildrenChanged(ChildrenChangedEventArgs args)
        {
            if (ChildrenChanged != null)
            {
                Task.Run( () => ChildrenChanged(this, args));
            }
        }

        #endregion

        public IEnumerable<string> PhysicalLocations { get; set; }

        public override bool IsFolder
        {
            get
            {
                return true;
            }
        }

        public bool IsRoot { get; set; }

        public bool IsVirtualFolder
        {
            get
            {
                return Parent != null && Parent.IsRoot;
            }
        }
        protected object childLock = new object();
        protected List<BaseItem> children;
        protected virtual List<BaseItem> ActualChildren
        {
            get
            {
                if (children == null)
                {
                    LoadChildren();
                }
                return children;
            }

            set
            {
                children = value;
            }
        }

        /// <summary>
        /// thread-safe access to the actual children of this folder - without regard to user
        /// </summary>
        public IEnumerable<BaseItem> Children
        {
            get
            {
                lock (childLock)
                    return ActualChildren.ToList();
            }
        }

        /// <summary>
        /// thread-safe access to all recursive children of this folder - without regard to user
        /// </summary>
        public IEnumerable<BaseItem> RecursiveChildren
        {
            get
            {
                foreach (var item in Children)
                {
                    yield return item;

                    var subFolder = item as Folder;

                    if (subFolder != null)
                    {
                        foreach (var subitem in subFolder.RecursiveChildren)
                        {
                            yield return subitem;
                        }
                    }
                }
            }
        }
                

        /// <summary>
        /// Loads and validates our children
        /// </summary>
        protected virtual void LoadChildren()
        {
            //first - load our children from the repo
            lock (childLock)
                children = GetCachedChildren();

            //then kick off a validation against the actual file system
            Task.Run(() => ValidateChildren());
        }

        protected bool ChildrenValidating = false;

        /// <summary>
        /// Compare our current children (presumably just read from the repo) with the current state of the file system and adjust for any changes
        /// ***Currently does not contain logic to maintain items that are unavailable in the file system***
        /// </summary>
        /// <returns></returns>
        protected async virtual void ValidateChildren()
        {
            if (ChildrenValidating) return; //only ever want one of these going at once and don't want them to fire off in sequence so don't use lock
            ChildrenValidating = true;
            bool changed = false; //this will save us a little time at the end if nothing changes
            var changedArgs = new ChildrenChangedEventArgs(this);
            //get the current valid children from filesystem (or wherever)
            var nonCachedChildren = await GetNonCachedChildren();
            if (nonCachedChildren == null) return; //nothing to validate
            //build a dictionary of the current children we have now by Id so we can compare quickly and easily
            Dictionary<Guid, BaseItem> currentChildren;
            lock (childLock)
                currentChildren =  ActualChildren.ToDictionary(i => i.Id);
            
            //create a list for our validated children
            var validChildren = new List<BaseItem>();
            //now traverse the valid children and find any changed or new items
            foreach (var child in nonCachedChildren)
            {
                BaseItem currentChild;
                currentChildren.TryGetValue(child.Id, out currentChild);
                if (currentChild == null)
                {
                    //brand new item - needs to be added
                    changed = true;
                    changedArgs.ItemsAdded.Add(child);
                    //Logger.LogInfo("New Item Added to Library: ("+child.GetType().Name+")"+ child.Name + "(" + child.Path + ")");
                    //refresh it
                    child.RefreshMetadata();
                    //save it in repo...

                    //and add it to our valid children
                    validChildren.Add(child);
                    //fire an added event...?
                    //if it is a folder we need to validate its children as well
                    Folder folder = child as Folder;
                    if (folder != null)
                    {
                        folder.ValidateChildren();
                        //probably need to refresh too...
                    }
                }
                else
                {
                    //existing item - check if it has changed
                    if (currentChild.IsChanged(child))
                    {
                        changed = true;
                        currentChild.RefreshMetadata();
                        //save it in repo...
                        validChildren.Add(currentChild);
                    }
                    else
                    {
                        //current child that didn't change - just put it in the valid children
                        validChildren.Add(currentChild);
                    }
                }
            }

            //that's all the new and changed ones - now see if there are any that are missing
            changedArgs.ItemsRemoved = currentChildren.Values.Except(validChildren);
            changed |= changedArgs.ItemsRemoved != null;

            //now, if anything changed - replace our children
            if (changed)
            {
                lock (childLock)
                    ActualChildren = validChildren;
                //and save children in repo...

                //and fire event
                this.OnChildrenChanged(changedArgs);
            }
            ChildrenValidating = false;

        }

        /// <summary>
        /// Get the children of this folder from the actual file system
        /// </summary>
        /// <returns></returns>
        protected async virtual Task<IEnumerable<BaseItem>> GetNonCachedChildren()
        {
            ItemResolveEventArgs args = new ItemResolveEventArgs()
            {
                FileInfo = FileData.GetFileData(this.Path),
                Parent = this.Parent,
                Cancel = false,
                Path = this.Path
            };

            // Gather child folder and files
            if (args.IsDirectory)
            {
                args.FileSystemChildren = FileData.GetFileSystemEntries(this.Path, "*").ToArray();

                bool isVirtualFolder = Parent != null && Parent.IsRoot;
                args = FileSystemHelper.FilterChildFileSystemEntries(args, isVirtualFolder);
            }
            else
            {
                Logger.LogError("Folder has a path that is not a directory: " + this.Path);
                return null;
            }

            if (!EntityResolutionHelper.ShouldResolvePathContents(args))
            {
                return null;
            }
            return (await Task.WhenAll<BaseItem>(GetChildren(args.FileSystemChildren)).ConfigureAwait(false))
                        .Where(i => i != null).OrderBy(f =>
                        {
                            return string.IsNullOrEmpty(f.SortName) ? f.Name : f.SortName;

                        });

        }

        /// <summary>
        /// Resolves a path into a BaseItem
        /// </summary>
        protected async Task<BaseItem> GetChild(string path,  WIN32_FIND_DATA? fileInfo = null)
        {
            ItemResolveEventArgs args = new ItemResolveEventArgs()
            {
                FileInfo = fileInfo ?? FileData.GetFileData(path),
                Parent = this,
                Cancel = false,
                Path = path
            };

            args.FileSystemChildren = FileData.GetFileSystemEntries(path, "*").ToArray();
            args = FileSystemHelper.FilterChildFileSystemEntries(args, false);

            return Kernel.Instance.ResolveItem(args);

        }

        /// <summary>
        /// Finds child BaseItems for a given Folder
        /// </summary>
        protected Task<BaseItem>[] GetChildren(WIN32_FIND_DATA[] fileSystemChildren)
        {
            Task<BaseItem>[] tasks = new Task<BaseItem>[fileSystemChildren.Length];

            for (int i = 0; i < fileSystemChildren.Length; i++)
            {
                var child = fileSystemChildren[i];

                tasks[i] = GetChild(child.Path, child);
            }

            return tasks;
        }


        /// <summary>
        /// Get our children from the repo - stubbed for now
        /// </summary>
        /// <returns></returns>
        protected virtual List<BaseItem> GetCachedChildren()
        {
            return new List<BaseItem>();
        }

        /// <summary>
        /// Gets allowed children of an item
        /// </summary>
        public IEnumerable<BaseItem> GetChildren(User user)
        {
            return ActualChildren.Where(c => c.IsParentalAllowed(user));
        }

        /// <summary>
        /// Gets allowed recursive children of an item
        /// </summary>
        public IEnumerable<BaseItem> GetRecursiveChildren(User user)
        {
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
        /// Since it can be slow to make all of these calculations at once, this method will provide a way to get them all back together
        /// </summary>
        public ItemSpecialCounts GetSpecialCounts(User user)
        {
            ItemSpecialCounts counts = new ItemSpecialCounts();

            IEnumerable<BaseItem> recursiveChildren = GetRecursiveChildren(user);

            counts.RecentlyAddedItemCount = GetRecentlyAddedItems(recursiveChildren, user).Count();
            counts.RecentlyAddedUnPlayedItemCount = GetRecentlyAddedUnplayedItems(recursiveChildren, user).Count();
            counts.InProgressItemCount = GetInProgressItems(recursiveChildren, user).Count();
            counts.PlayedPercentage = GetPlayedPercentage(recursiveChildren, user);

            return counts;
        }

        /// <summary>
        /// Finds all recursive items within a top-level parent that contain the given genre and are allowed for the current user
        /// </summary>
        public IEnumerable<BaseItem> GetItemsWithGenre(string genre, User user)
        {
            return GetRecursiveChildren(user).Where(f => f.Genres != null && f.Genres.Any(s => s.Equals(genre, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Finds all recursive items within a top-level parent that contain the given year and are allowed for the current user
        /// </summary>
        public IEnumerable<BaseItem> GetItemsWithYear(int year, User user)
        {
            return GetRecursiveChildren(user).Where(f => f.ProductionYear.HasValue && f.ProductionYear == year);
        }

        /// <summary>
        /// Finds all recursive items within a top-level parent that contain the given studio and are allowed for the current user
        /// </summary>
        public IEnumerable<BaseItem> GetItemsWithStudio(string studio, User user)
        {
            return GetRecursiveChildren(user).Where(f => f.Studios != null && f.Studios.Any(s => s.Equals(studio, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Finds all recursive items within a top-level parent that the user has marked as a favorite
        /// </summary>
        public IEnumerable<BaseItem> GetFavoriteItems(User user)
        {
            return GetRecursiveChildren(user).Where(c =>
            {
                UserItemData data = c.GetUserData(user, false);

                if (data != null)
                {
                    return data.IsFavorite;
                }

                return false;
            });
        }

        /// <summary>
        /// Finds all recursive items within a top-level parent that contain the given person and are allowed for the current user
        /// </summary>
        public IEnumerable<BaseItem> GetItemsWithPerson(string person, User user)
        {
            return GetRecursiveChildren(user).Where(c =>
            {
                if (c.People != null)
                {
                    return c.People.ContainsKey(person);
                }

                return false;
            });
        }

        /// <summary>
        /// Finds all recursive items within a top-level parent that contain the given person and are allowed for the current user
        /// </summary>
        /// <param name="personType">Specify this to limit results to a specific PersonType</param>
        public IEnumerable<BaseItem> GetItemsWithPerson(string person, string personType, User user)
        {
            return GetRecursiveChildren(user).Where(c =>
            {
                if (c.People != null)
                {
                    return c.People.ContainsKey(person) && c.People[person].Type.Equals(personType, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            });
        }

        /// <summary>
        /// Gets all recently added items (recursive) within a folder, based on configuration and parental settings
        /// </summary>
        public IEnumerable<BaseItem> GetRecentlyAddedItems(User user)
        {
            return GetRecentlyAddedItems(GetRecursiveChildren(user), user);
        }

        /// <summary>
        /// Gets all recently added unplayed items (recursive) within a folder, based on configuration and parental settings
        /// </summary>
        public IEnumerable<BaseItem> GetRecentlyAddedUnplayedItems(User user)
        {
            return GetRecentlyAddedUnplayedItems(GetRecursiveChildren(user), user);
        }

        /// <summary>
        /// Gets all in-progress items (recursive) within a folder
        /// </summary>
        public IEnumerable<BaseItem> GetInProgressItems(User user)
        {
            return GetInProgressItems(GetRecursiveChildren(user), user);
        }

        /// <summary>
        /// Takes a list of items and returns the ones that are recently added
        /// </summary>
        private static IEnumerable<BaseItem> GetRecentlyAddedItems(IEnumerable<BaseItem> itemSet, User user)
        {
            return itemSet.Where(i => !(i.IsFolder) && i.IsRecentlyAdded(user));
        }

        /// <summary>
        /// Takes a list of items and returns the ones that are recently added and unplayed
        /// </summary>
        private static IEnumerable<BaseItem> GetRecentlyAddedUnplayedItems(IEnumerable<BaseItem> itemSet, User user)
        {
            return GetRecentlyAddedItems(itemSet, user).Where(i =>
            {
                var userdata = i.GetUserData(user, false);

                return userdata == null || userdata.PlayCount == 0;
            });
        }

        /// <summary>
        /// Takes a list of items and returns the ones that are in progress
        /// </summary>
        private static IEnumerable<BaseItem> GetInProgressItems(IEnumerable<BaseItem> itemSet, User user)
        {
            return itemSet.Where(i =>
            {
                if (i.IsFolder)
                {
                    return false;
                }

                var userdata = i.GetUserData(user, false);

                return userdata != null && userdata.PlaybackPositionTicks > 0;
            });
        }

        /// <summary>
        /// Gets the total played percentage for a set of items
        /// </summary>
        private static decimal GetPlayedPercentage(IEnumerable<BaseItem> itemSet, User user)
        {
            itemSet = itemSet.Where(i => !(i.IsFolder));

            if (!itemSet.Any())
            {
                return 0;
            }

            decimal totalPercent = 0;

            foreach (BaseItem item in itemSet)
            {
                UserItemData data = item.GetUserData(user, false);

                if (data == null)
                {
                    continue;
                }

                if (data.PlayCount > 0)
                {
                    totalPercent += 100;
                }
                else if (data.PlaybackPositionTicks > 0 && item.RunTimeTicks.HasValue)
                {
                    decimal itemPercent = data.PlaybackPositionTicks;
                    itemPercent /= item.RunTimeTicks.Value;
                    totalPercent += itemPercent;
                }
            }

            return totalPercent / itemSet.Count();
        }

        /// <summary>
        /// Marks the item as either played or unplayed
        /// </summary>
        public override void SetPlayedStatus(User user, bool wasPlayed)
        {
            base.SetPlayedStatus(user, wasPlayed);

            // Now sweep through recursively and update status
            foreach (BaseItem item in GetChildren(user))
            {
                item.SetPlayedStatus(user, wasPlayed);
            }
        }

        /// <summary>
        /// Finds an item by ID, recursively
        /// </summary>
        public override BaseItem FindItemById(Guid id)
        {
            var result = base.FindItemById(id);

            if (result != null)
            {
                return result;
            }

            foreach (BaseItem item in ActualChildren)
            {
                result = item.FindItemById(id);

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
        public BaseItem FindByPath(string path)
        {
            if (Path.Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                return this;
            }

            foreach (BaseItem item in ActualChildren)
            {
                var folder = item as Folder;

                if (folder != null)
                {
                    var foundItem = folder.FindByPath(path);

                    if (foundItem != null)
                    {
                        return foundItem;
                    }
                }
                else if (item.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }

            return null;
        }
    }
}
