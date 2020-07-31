using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing whether the associated user has a specific permission.
    /// </summary>
    public partial class Permission : ISavingChanges
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Permission"/> class.
        /// Public constructor with required data.
        /// </summary>
        /// <param name="kind">The permission kind.</param>
        /// <param name="value">The value of this permission.</param>
        public Permission(PermissionKind kind, bool value)
        {
            Kind = kind;
            Value = value;

            Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Permission"/> class.
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Permission()
        {
            Init();
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Gets or sets the id of this permission.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        /// <summary>
        /// Gets or sets the type of this permission.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        [Required]
        public PermissionKind Kind { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether the associated user has this permission.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        [Required]
        public bool Value { get; set; }

        /// <summary>
        /// Gets or sets the row version.
        /// </summary>
        /// <remarks>
        /// Required, ConcurrencyToken.
        /// </remarks>
        [ConcurrencyCheck]
        [Required]
        public uint RowVersion { get; set; }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="kind">The permission kind.</param>
        /// <param name="value">The value of this permission.</param>
        /// <returns>The newly created instance.</returns>
        public static Permission Create(PermissionKind kind, bool value)
        {
            return new Permission(kind, value);
        }

        /// <inheritdoc/>
        public void OnSavingChanges()
        {
            RowVersion++;
        }

        partial void Init();
    }
}
