#nullable disable

using System;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Sorting
{
    /// <summary>
    /// Class DatePlayedComparer.
    /// </summary>
    public class UserRatingComparer : IUserBaseItemComparer
    {
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public User User { get; set; }

        /// <summary>
        /// Gets or sets the user manager.
        /// </summary>
        /// <value>The user manager.</value>
        public IUserManager UserManager { get; set; }

        /// <summary>
        /// Gets or sets the user data repository.
        /// </summary>
        /// <value>The user data repository.</value>
        public IUserDataManager UserDataRepository { get; set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => ItemSortBy.UserRating;

        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return GetUserRating(x).CompareTo(GetUserRating(y));
        }

        /// <summary>
        /// Gets the UserRating.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>DateTime.</returns>
        private int GetUserRating(BaseItem x)
        {
            var userdata = UserDataRepository.GetUserData(User, x);

            if (userdata is not null && userdata.LastPlayedDate.HasValue)
            {
                return userdata.UserRating.Value;
            }

            return 0;
        }
    }
}
