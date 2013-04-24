
namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class Artist
    /// </summary>
    public class Artist : BaseItem
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "Artist-" + Name;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is on tour.
        /// </summary>
        /// <value><c>true</c> if this instance is on tour; otherwise, <c>false</c>.</value>
        public bool IsOnTour { get; set; }
    }
}
