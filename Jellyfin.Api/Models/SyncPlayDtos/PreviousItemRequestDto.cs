namespace Jellyfin.Api.Models.SyncPlayDtos
{
    /// <summary>
    /// Class PreviousItemRequestDto.
    /// </summary>
    public class PreviousItemRequestDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreviousItemRequestDto"/> class.
        /// </summary>
        public PreviousItemRequestDto()
        {
            PlaylistItemId = string.Empty;
        }

        /// <summary>
        /// Gets or sets the playing item identifier.
        /// </summary>
        /// <value>The playing item identifier.</value>
        public string PlaylistItemId { get; set; }
    }
}
