
namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Genre
    /// </summary>
    public class Genre : BaseItem
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return Name;
        }
    }
}
