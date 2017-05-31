using System.Diagnostics;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class StudioDto
    /// </summary>
    [DebuggerDisplay("Name = {Name}")]
    public class StudioDto
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }
        
        /// <summary>
        /// Gets or sets the primary image tag.
        /// </summary>
        /// <value>The primary image tag.</value>
        public string PrimaryImageTag { get; set; }
    }
}