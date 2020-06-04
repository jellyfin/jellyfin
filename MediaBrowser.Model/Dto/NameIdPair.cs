#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Dto
{
    public class NameIdPair
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
    }

    public class NameGuidPair
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
    }
}
