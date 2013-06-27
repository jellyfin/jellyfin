
namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Year
    /// </summary>
    public class Year : BaseItem, IItemByName
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "Year-" + Name;
        }
    }
}
