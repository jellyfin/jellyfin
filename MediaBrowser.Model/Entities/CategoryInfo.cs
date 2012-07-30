
namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// This is a stub class used by the api to get IBN types along with their item counts
    /// </summary>
    public class CategoryInfo<T>
    {
        /// <summary>
        /// The actual genre, year, studio, etc
        /// </summary>
        public T Item { get; set; }

        /// <summary>
        /// The number of items that have the genre, year, studio, etc
        /// </summary>
        public int ItemCount { get; set; }
    }
}
