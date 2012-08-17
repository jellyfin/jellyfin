using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Model.Entities
{
    public class Folder : BaseItem
    {
        public bool IsRoot { get; set; }

        public bool IsVirtualFolder
        {
            get
            {
                return Parent != null && Parent.IsRoot;
            }
        }

        [IgnoreDataMember]
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
        /// <param name="personType">Specify this to limit results to a specific PersonType</param>
        public IEnumerable<BaseItem> GetItemsWithPerson(string person, PersonType? personType, User user)
        {
            return GetParentalAllowedRecursiveChildren(user).Where(c =>
            {
                if (c.People != null)
                {
                    if (personType.HasValue)
                    {
                        return c.People.Any(p => p.Name.Equals(person, StringComparison.OrdinalIgnoreCase) && p.PersonType == personType.Value);
                    }
                    else
                    {
                        return c.People.Any(p => p.Name.Equals(person, StringComparison.OrdinalIgnoreCase));
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// Gets all recently added items (recursive) within a folder, based on configuration and parental settings
        /// </summary>
        public IEnumerable<BaseItem> GetRecentlyAddedItems(User user)
        {
            DateTime now = DateTime.Now;

            return GetParentalAllowedRecursiveChildren(user).Where(i => !(i is Folder) && (now - i.DateCreated).TotalDays < user.RecentItemDays);
        }

        /// <summary>
        /// Gets all recently added unplayed items (recursive) within a folder, based on configuration and parental settings
        /// </summary>
        public IEnumerable<BaseItem> GetRecentlyAddedUnplayedItems(User user)
        {
            return GetRecentlyAddedItems(user).Where(i =>
            {
                var userdata = user.GetItemData(i.Id);

                return userdata == null || userdata.PlayCount == 0;
            });
        }

        /// <summary>
        /// Gets all in-progress items (recursive) within a folder
        /// </summary>
        public IEnumerable<BaseItem> GetInProgressItems(User user)
        {
            return GetParentalAllowedRecursiveChildren(user).Where(i =>
            {
                if (i is Folder)
                {
                    return false;
                }

                var userdata = user.GetItemData(i.Id);

                return userdata != null && userdata.PlaybackPosition.Ticks > 0;
            });
        }

        /// <summary>
        /// Finds an item by ID, recursively
        /// </summary>
        public BaseItem FindById(Guid id)
        {
            if (Id == id)
            {
                return this;
            }

            foreach (BaseItem item in Children)
            {
                var folder = item as Folder;

                if (folder != null)
                {
                    var foundItem = folder.FindById(id);

                    if (foundItem != null)
                    {
                        return foundItem;
                    }
                }
                else if (item.Id == id)
                {
                    return item;
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
