namespace Jellyfin.KodiMetadata.Models
{
    /// <summary>
    /// The actor nfo tag.
    /// </summary>
    public class ActorNfo
    {
        /// <summary>
        /// Gets or sets the actor name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the actor role.
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// Gets or sets the actor picture.
        /// </summary>
        public string? Thumb { get; set; }

        /// <summary>
        /// Gets or sets the order in which the actors should appear.
        /// </summary>
        public int? Order { get; set; }
    }
}
