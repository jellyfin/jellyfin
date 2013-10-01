
namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicArtist
    /// </summary>
    public class MusicArtist : Folder
    {
        /// <summary>
        /// Gets or sets the last fm image URL.
        /// </summary>
        /// <value>The last fm image URL.</value>
        public string LastFmImageUrl { get; set; }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return Artist.GetUserDataKey(this);
        }
    }
}
