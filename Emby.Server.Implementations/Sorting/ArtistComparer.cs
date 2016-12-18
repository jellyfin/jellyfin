using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Querying;
using System;

namespace Emby.Server.Implementations.Sorting
{
    /// <summary>
    /// Class ArtistComparer
    /// </summary>
    public class ArtistComparer : IBaseItemComparer
    {
        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y)
        {
            return string.Compare(GetValue(x), GetValue(y), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.String.</returns>
        private string GetValue(BaseItem x)
        {
            var audio = x as Audio;

            if (audio == null)
            {
                return string.Empty;
            }

            return audio.Artists.Count == 0 ? null : audio.Artists[0];
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get { return ItemSortBy.Artist; }
        }
    }
}
