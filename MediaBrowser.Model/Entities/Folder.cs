using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.Entities
{
    public class Folder : BaseItem
    {
        public bool IsRoot { get; set; }

        public BaseItem[] Children { get; set; }

        /// <summary>
        /// Gets allowed children of an item
        /// </summary>
        public IEnumerable<BaseItem> GetParentalAllowedChildren(User user)
        {
            return Children.Where(c => c.IsParentalAllowed(user));
        }

        /// <summary>
        /// Gets allowed recursive children of an item
        /// </summary>
        public IEnumerable<BaseItem> GetParentalAllowedRecursiveChildren(User user)
        {
            foreach (var item in GetParentalAllowedChildren(user))
            {
                yield return item;

                var subFolder = item as Folder;

                if (subFolder != null)
                {
                    foreach (var subitem in subFolder.GetParentalAllowedRecursiveChildren(user))
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

            IEnumerable<BaseItem> recursiveChildren = GetParentalAllowedRecursiveChildren(user);

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
            return GetParentalAllowedRecursiveChildren(user).Where(f => f.Genres != null && f.Genres.Any(s => s.Equals(genre, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Finds all recursive items within a top-level parent that contain the given year and are allowed for the current user
        /// </summary>
        public IEnumerable<BaseItem> GetItemsWithYear(int year, User user)
        {
            return GetParentalAllowedRecursiveChildren(user).Where(f => f.ProductionYear.HasValue && f.ProductionYear == year);
        }

        /// <summary>
        /// Finds all recursive items within a top-level parent that contain the given studio and are allowed for the current user
        /// </summary>
        public IEnumerable<BaseItem> GetItemsWithStudio(string studio, User user)
        {
            return GetParentalAllowedRecursiveChildren(user).Where(f => f.Studios != null && f.Studios.Any(s => s.Equals(studio, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Finds all recursive items within a top-level parent that contain the given person and are allowed for the current user
        /// </summary>
        public IEnumerable<BaseItem> GetItemsWithPerson(string person, User user)
        {
            return GetParentalAllowedRecursiveChildren(user).Where(c =>
            {
                if (c.People != null)
                {
                    return c.People.Any(p => p.Name.Equals(person, StringComparison.OrdinalIgnoreCase));
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
            return GetParentalAllowedRecursiveChildren(user).Where(c =>
            {
                if (c.People != null)
                {
                    return c.People.Any(p => p.Name.Equals(person, StringComparison.OrdinalIgnoreCase) && p.Type == personType);
                }

                return false;
            });
        }

        /// <summary>
        /// Gets all recently added items (recursive) within a folder, based on configuration and parental settings
        /// </summary>
        public IEnumerable<BaseItem> GetRecentlyAddedItems(User user)
        {
            return GetRecentlyAddedItems(GetParentalAllowedRecursiveChildren(user), user);
        }

        /// <summary>
        /// Gets all recently added unplayed items (recursive) within a folder, based on configuration and parental settings
        /// </summary>
        public IEnumerable<BaseItem> GetRecentlyAddedUnplayedItems(User user)
        {
            return GetRecentlyAddedUnplayedItems(GetParentalAllowedRecursiveChildren(user), user);
        }

        /// <summary>
        /// Gets all in-progress items (recursive) within a folder
        /// </summary>
        public IEnumerable<BaseItem> GetInProgressItems(User user)
        {
            return GetInProgressItems(GetParentalAllowedRecursiveChildren(user), user);
        }

        private static IEnumerable<BaseItem> GetRecentlyAddedItems(IEnumerable<BaseItem> itemSet, User user)
        {
            return itemSet.Where(i => !(i is Folder) && i.IsRecentlyAdded(user));
        }

        private static IEnumerable<BaseItem> GetRecentlyAddedUnplayedItems(IEnumerable<BaseItem> itemSet, User user)
        {
            return GetRecentlyAddedItems(itemSet, user).Where(i =>
            {
                var userdata = i.GetUserData(user);

                return userdata == null || userdata.PlayCount == 0;
            });
        }

        private static IEnumerable<BaseItem> GetInProgressItems(IEnumerable<BaseItem> itemSet, User user)
        {
            return itemSet.Where(i =>
            {
                if (i is Folder)
                {
                    return false;
                }

                var userdata = i.GetUserData(user);

                return userdata != null && userdata.PlaybackPositionTicks > 0;
            });
        }

        private static decimal GetPlayedPercentage(IEnumerable<BaseItem> itemSet, User user)
        {
            itemSet = itemSet.Where(i => !(i is Folder));

            if (!itemSet.Any())
            {
                return 0;
            }

            decimal totalPercent = 0;

            foreach (BaseItem item in itemSet)
            {
                UserItemData data = item.GetUserData(user);

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
        /// Finds an item by ID, recursively
        /// </summary>
        public override BaseItem FindItemById(Guid id)
        {
            var result = base.FindItemById(id);

            if (result != null)
            {
                return result;
            }

            foreach (BaseItem item in Children)
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

            foreach (BaseItem item in Children)
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
