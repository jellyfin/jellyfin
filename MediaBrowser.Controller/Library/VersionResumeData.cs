using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// The user data of the alternate version that should drive resume for a multi-version item.
    /// </summary>
    /// <param name="UserData">The resume version's user data.</param>
    /// <param name="RunTimeTicks">The resume version's runtime, used for the progress percentage.</param>
    public record VersionResumeData(UserItemData UserData, long? RunTimeTicks)
    {
        /// <summary>
        /// Applies the resume version's playback state to the supplied user data dto, so that an item
        /// whose most recent progress lives on an alternate version still reports that progress.
        /// </summary>
        /// <param name="dto">The user data dto to update.</param>
        public void ApplyTo(UserItemDataDto dto)
        {
            dto.PlaybackPositionTicks = UserData.PlaybackPositionTicks;
            dto.Played = UserData.Played;
            dto.LastPlayedDate = UserData.LastPlayedDate;

            if (RunTimeTicks > 0 && UserData.PlaybackPositionTicks > 0)
            {
                dto.PlayedPercentage = 100.0 * UserData.PlaybackPositionTicks / RunTimeTicks.Value;
            }
        }
    }
}
