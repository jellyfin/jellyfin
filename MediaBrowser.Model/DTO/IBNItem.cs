using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.DTO
{
    /// <summary>
    /// This is a stub class used by the api to get IBN types along with their item counts
    /// </summary>
    public class IBNItem<T>
    {
        /// <summary>
        /// The actual genre, year, studio, etc
        /// </summary>
        public T Item { get; set; }

        /// <summary>
        /// The number of items that have the genre, year, studio, etc
        /// </summary>
        public int BaseItemCount { get; set; }
    }

    /// <summary>
    /// This is used by BaseItemContainer
    /// </summary>
    public class BaseItemPerson
    {
        public PersonInfo PersonInfo { get; set; }
        public bool HasImage { get; set; }
    }

    /// <summary>
    /// This is used by BaseItemContainer
    /// </summary>
    public class BaseItemStudio
    {
        public string Name { get; set; }
        public bool HasImage { get; set; }
    }
}
