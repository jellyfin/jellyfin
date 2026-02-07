using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Database.Implementations.Enums;

namespace Jellyfin.Database.Implementations.Entities
{
    /// <summary>
    /// An entity representing a user's home section.
    /// </summary>
    public class UserHomeSection
    {
        /// <summary>
        /// Gets the Id.
        /// </summary>
        /// <remarks>
        /// Identity. Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the user Id.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the section Id.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public Guid SectionId { get; set; }

        /// <summary>
        /// Gets or sets the name of the section.
        /// </summary>
        /// <remarks>
        /// Required. Max Length = 64.
        /// </remarks>
        [MaxLength(64)]
        [StringLength(64)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the section.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public HomeSectionType SectionType { get; set; }

        /// <summary>
        /// Gets or sets the priority/order of this section.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public int Priority { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items to display in the section.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public int MaxItems { get; set; }

        /// <summary>
        /// Gets or sets the sort order for items in this section.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public SortOrder SortOrder { get; set; }

        /// <summary>
        /// Gets or sets how items should be sorted in this section.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public int SortBy { get; set; }
    }
}
