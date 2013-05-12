
namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicAlbumDisc
    /// </summary>
    public class MusicAlbumDisc : Folder
    {
        /// <summary>
        /// Gets or sets the display type of the media.
        /// </summary>
        /// <value>The display type of the media.</value>
        public override string DisplayMediaType
        {
            get
            {
                return "Disc";
            }
            set
            {
                base.DisplayMediaType = value;
            }
        }
    }
}
