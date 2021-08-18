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
        /// Gets the id of this preference.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the id of the associated user.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets the type of this preference.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public PreferenceKind Kind { get; private set; }

        /// <summary>
        /// Gets or sets the value of this preference.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 65535.
        /// </remarks>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Value { get; set; }

        /// <inheritdoc/>
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <inheritdoc/>
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
