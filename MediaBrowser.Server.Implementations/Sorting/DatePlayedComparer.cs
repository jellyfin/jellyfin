using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Dto;
using System;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Server.Implementations.Sorting
{
    /// <summary>
    /// Class DatePlayedComparer
    /// </summary>
    public class DatePlayedComparer : IUserBaseItemComparer
    {
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        public User User { get; set; }

        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return GetDate(x).CompareTo(GetDate(y));
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>DateTime.</returns>
        private DateTime GetDate(BaseItem x)
        {
            var userdata = x.GetUserData(User, false);

            if (userdata != null && userdata.LastPlayedDate.HasValue)
            {
                return userdata.LastPlayedDate.Value;
            }

            return DateTime.MaxValue;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return ItemSortBy.DatePlayed; }
        }
    }
}
