#nullable disable
namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Class BrowseRequest.
    /// </summary>
    public class BrowseRequest
    {
        /// <summary>
        /// Artist, Genre, Studio, Person, or any kind of BaseItem.
        /// </summary>
        /// <value>The type of the item.</value>
        public string ItemType { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The item id.</value>
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        /// <value>The name of the item.</value>
        public string ItemName { get; set; }
    }
}
