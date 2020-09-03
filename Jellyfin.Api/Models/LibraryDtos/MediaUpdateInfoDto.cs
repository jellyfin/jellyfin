namespace Jellyfin.Api.Models.LibraryDtos
{
    /// <summary>
    /// Media Update Info Dto.
    /// </summary>
    public class MediaUpdateInfoDto
    {
        /// <summary>
        /// Gets or sets media path.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Gets or sets media update type.
        /// Created, Modified, Deleted.
        /// </summary>
        public string? UpdateType { get; set; }
    }
}
