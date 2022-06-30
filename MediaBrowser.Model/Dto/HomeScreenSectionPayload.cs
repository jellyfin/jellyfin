using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Payload created by provided data from the frontend to determine how to deal with how a home screen section should display.
    /// </summary>
    public class HomeScreenSectionPayload
    {
        /// <summary>
        /// The UserId that's requesting the section data.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Additional data is used to make the specific request more dynamic. It could be GetLatestMedia (tvshows/movies) or suggestions based on a genre.
        /// There is no limit to what this value could be.
        /// </summary>
        public string? AdditionalData { get; set; }
    }
}
