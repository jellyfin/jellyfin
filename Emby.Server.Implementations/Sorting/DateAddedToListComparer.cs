#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Sorting;

namespace Emby.Server.Implementations.Sorting
{
    public class DateAddedToListComparer : IUserListBaseItemComparer
    {
        private readonly Lazy<IReadOnlyDictionary<Guid, DateTime>> _itemDates;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateAddedToListComparer"/> class.
        /// </summary>
        public DateAddedToListComparer()
        {
            _itemDates = new Lazy<IReadOnlyDictionary<Guid, DateTime>>(
                () => ResolveItemDatesAsync().GetAwaiter().GetResult());
        }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public User User { get; set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public ItemSortBy Type => ItemSortBy.DateAddedToList;

        /// <summary>
        /// Gets or sets the user data manager.
        /// </summary>
        /// <value>The user data manager.</value>
        public IUserDataManager UserDataManager { get; set; }

        /// <summary>
        /// Gets or sets the user list manager.
        /// </summary>
        /// <value>The user list manager.</value>
        public IUserListManager UserListManager { get; set; }

        /// <summary>
        /// Gets or sets the user manager.
        /// </summary>
        /// <value>The user manager.</value>
        public IUserManager UserManager { get; set; }

        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return GetValue(x).CompareTo(GetValue(y));
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>DateTime.</returns>
        private DateTime GetValue(BaseItem x)
        {
            return _itemDates.Value.TryGetValue(x.Id, out var dateAdded)
                ? dateAdded
                : DateTime.MinValue;
        }

        private async Task<IReadOnlyDictionary<Guid, DateTime>> ResolveItemDatesAsync()
        {
            var defaultList = await UserListManager
                .GetOrCreateDefaultListAsync(User.Id)
                .ConfigureAwait(false);

            return await UserListManager
                .GetListItemDatesAsync(defaultList.Id)
                .ConfigureAwait(false);
        }
    }
}
