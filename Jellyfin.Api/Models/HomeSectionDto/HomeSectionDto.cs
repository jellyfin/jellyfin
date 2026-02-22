using System;
using System.ComponentModel.DataAnnotations;
using MediaBrowser.Model.Configuration;

namespace Jellyfin.Api.Models.HomeSectionDto
{
    /// <summary>
    /// Home section DTO.
    /// </summary>
    public class HomeSectionDto
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Gets or sets the section options.
        /// </summary>
        [Required]
        public HomeSectionOptions SectionOptions { get; set; } = new HomeSectionOptions();
    }
}
