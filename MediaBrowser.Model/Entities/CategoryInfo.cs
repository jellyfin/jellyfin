
namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// This is a stub class used by the api to get IBN types in a compact format
    /// </summary>
    public class CategoryInfo
    {
        /// <summary>
        /// The name of the genre, year, studio, etc
        /// </summary>
        public string Name { get; set; }

        public string PrimaryImagePath { get; set; }

        /// <summary>
        /// The number of items that have the genre, year, studio, etc
        /// </summary>
        public int ItemCount { get; set; }
    }
}
