using Jellyfin.Api.Models.SyncPlay;

namespace Jellyfin.Api.Models.SyncPlay.Dtos
{
    /// <summary>
    /// Class SetRepeatModeRequestDto.
    /// </summary>
    public class SetRepeatModeRequestDto
    {
        /// <summary>
        /// Gets or sets the repeat mode.
        /// </summary>
        /// <value>The repeat mode.</value>
        public GroupRepeatMode Mode { get; set; }
    }
}
