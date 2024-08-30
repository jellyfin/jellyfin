using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Jellyfin.Data.Entities.Security
{
    /// <summary>
    /// An entity representing an API key.
    /// </summary>
    public class ApiKey
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiKey"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ApiKey(string name)
        {
            Name = name;

            AccessToken = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            DateCreated = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the date of last activity.
        /// </summary>
        public DateTime DateLastActivity { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [MaxLength(64)]
        [StringLength(64)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string AccessToken { get; set; }
    }
}
