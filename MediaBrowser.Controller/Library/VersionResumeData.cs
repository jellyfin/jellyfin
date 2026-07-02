using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// The user data of the most recently played alternate version that should drive the completion state of a multi-version item.
    /// </summary>
    /// <param name="UserData">The resume version's user data.</param>
    public record VersionResumeData(UserItemData UserData)
    {
        /// <summary>
        /// Merges the most recently played version's completion state into the supplied user data dto.
        /// Only completion (played) propagates to the primary; the in-progress resume position stays on
        /// the version that owns it, which is surfaced directly (e.g. in resume queries) so that playback
        /// always targets the correct version rather than resuming the primary at another version's offset.
        /// </summary>
        /// <param name="dto">The user data dto to update.</param>
        public void ApplyTo(UserItemDataDto dto)
        {
            dto.Played = dto.Played || UserData.Played;

            if ((UserData.LastPlayedDate ?? DateTime.MinValue) > (dto.LastPlayedDate ?? DateTime.MinValue))
            {
                dto.LastPlayedDate = UserData.LastPlayedDate;
            }
        }
    }
}
