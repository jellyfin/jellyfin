using System;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class ChildDefinition
    /// </summary>
    public class ChildDefinition
    {
        /// <summary>
        /// Gets or sets the item id.
        /// </summary>
        /// <value>The item id.</value>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }
    }
}
