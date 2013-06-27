
namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicGenre
    /// </summary>
    public class MusicGenre : BaseItem, IItemByName
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "MusicGenre-" + Name;
        }
    }
}
