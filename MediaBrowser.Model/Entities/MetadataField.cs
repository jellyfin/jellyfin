namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Enum MetadataFields.
    /// </summary>
    public enum MetadataField
    {
        /// <summary>
        /// The cast.
        /// </summary>
        Cast,

        /// <summary>
        /// The genres.
        /// </summary>
        Genres,

        /// <summary>
        /// The production locations.
        /// </summary>
        ProductionLocations,

        /// <summary>
        /// The companies.
        /// </summary>
        Companies,

        /// <summary>
        /// The tags.
        /// </summary>
        Tags,

        /// <summary>
        /// The name.
        /// </summary>
        Name,

        /// <summary>
        /// The overview.
        /// </summary>
        Overview,

        /// <summary>
        /// The runtime.
        /// </summary>
        Runtime,

        /// <summary>
        /// The official rating.
        /// </summary>
        OfficialRating,

        /// <summary>
        /// The studios.
        /// </summary>
        /// <remarks>
        /// Kept for compatibility with older clients; use <see cref="Companies"/>. Declared last
        /// because the members above it take their values from their position.
        /// </remarks>
        Studios = Companies
    }
}
