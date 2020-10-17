using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing a preference attached to a user or group.
    /// </summary>
    public class Preference : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Preference"/> class.
        /// Public constructor with required data.
        /// </summary>
        /// <param name="kind">The preference kind.</param>
        /// <param name="value">The value.</param>
        public Preference(PreferenceKind kind, string value)
        {
            Kind = kind;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Preference"/> class.
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Preference()
        {
        }

        /// <summary>
        /// Gets or sets the id of this preference.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        /// <summary>
        /// Gets or sets the type of this preference.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public PreferenceKind Kind { get; protected set; }

        /// <summary>
        /// Gets or sets the value of this preference.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 65535.
        /// </remarks>
        [Required]
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Value { get; set; }

        /// <inheritdoc/>
        [ConcurrencyCheck]
        public uint RowVersion { get; set; }

        /// <inheritdoc/>
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
