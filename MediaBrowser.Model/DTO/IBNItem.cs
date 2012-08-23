using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Model.DTO
{
    /// <summary>
    /// This is a stub class used by the api to get IBN types along with their item counts
    /// </summary>
    public class IBNItem
    {
        /// <summary>
        /// The name of the person, genre, etc
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The id of the person, genre, etc
        /// </summary>
        public Guid Id { get; set; }

        public bool HasImage { get; set; }

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
        public string Name { get; set; }
        public string Overview { get; set; }
        public string Type { get; set; }
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
