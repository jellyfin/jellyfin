
namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Studio
    /// </summary>
    public class Studio : BaseItem, IItemByName
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "Studio-" + Name;
        }
    }
}
