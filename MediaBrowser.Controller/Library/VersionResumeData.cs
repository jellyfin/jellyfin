using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// The user data of the most recently played alternate version that should drive the completion state of a multi-version item.
    /// </summary>
    /// <param name="VersionId">The id of the version that owns <paramref name="UserData"/>.</param>
    /// <param name="UserData">The resume version's user data.</param>
    public record VersionResumeData(Guid VersionId, UserItemData UserData)
    {
        /// <summary>
        /// Merges the most recently played version's completion state into the supplied user data dto.
        /// Completion (played) propagates to the primary. An in-progress resume position stays on the version
        /// that owns it, which is surfaced directly (e.g. in resume queries) so that playback always targets
        /// the correct version rather than resuming the primary at another version's offset. When the movie was
        /// finished on a different version, the primary's own stale resume position is cleared so it does not
        /// render as "watched and resumable" at the same time.
        /// </summary>
        /// <param name="dto">The user data dto to update.</param>
        public void ApplyTo(UserItemDataDto dto)
        {
            dto.Played = dto.Played || UserData.Played;

            if ((UserData.LastPlayedDate ?? DateTime.MinValue) > (dto.LastPlayedDate ?? DateTime.MinValue))
            {
                dto.LastPlayedDate = UserData.LastPlayedDate;
            }

            // A different version was finished (played, no resume position of its own) and is the most
            // recently played: the whole movie is watched.
            if (!VersionId.Equals(dto.ItemId) && UserData.Played && UserData.PlaybackPositionTicks <= 0)
            {
                dto.PlaybackPositionTicks = 0;
                dto.PlayedPercentage = null;
            }
        }
    }
}
