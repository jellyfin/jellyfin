using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing a section on the user's home page.
    /// </summary>
    public class HomeSection
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <remarks>
        /// Identity. Required.
        /// </remarks>
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        /// <summary>
        /// Gets or sets the Id of the associated display preferences.
        /// </summary>
        public int DisplayPreferencesId { get; set; }

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        public HomeSectionType Type { get; set; }
    }
}
