using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing a group.
    /// </summary>
    public partial class Group : IHasPermissions, ISavingChanges
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Group"/> class.
        /// Public constructor with required data.
        /// </summary>
        /// <param name="name">The name of the group.</param>
        public Group(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Id = Guid.NewGuid();

            Permissions = new HashSet<Permission>();
            ProviderMappings = new HashSet<ProviderMapping>();
            Preferences = new HashSet<Preference>();

            Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Group"/> class.
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Group()
        {
            Init();
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Gets or sets the id of this group.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [Key]
        [Required]
        public Guid Id { get; protected set; }

        /// <summary>
        /// Gets or sets the group's name.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 255.
        /// </remarks>
        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the row version.
        /// </summary>
        /// <remarks>
        /// Required, Concurrency Token.
        /// </remarks>
        [ConcurrencyCheck]
        [Required]
        public uint RowVersion { get; set; }

        public void OnSavingChanges()
        {
            RowVersion++;
        }

        /*************************************************************************
         * Navigation properties
         *************************************************************************/

        [ForeignKey("Permission_GroupPermissions_Id")]
        public virtual ICollection<Permission> Permissions { get; protected set; }

        [ForeignKey("ProviderMapping_ProviderMappings_Id")]
        public virtual ICollection<ProviderMapping> ProviderMappings { get; protected set; }

        [ForeignKey("Preference_Preferences_Id")]
        public virtual ICollection<Preference> Preferences { get; protected set; }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="name">The name of this group.</param>
        public static Group Create(string name)
        {
            return new Group(name);
        }

        /// <inheritdoc/>
        public bool HasPermission(PermissionKind kind)
        {
            return Permissions.First(p => p.Kind == kind).Value;
        }

        /// <inheritdoc/>
        public void SetPermission(PermissionKind kind, bool value)
        {
            Permissions.First(p => p.Kind == kind).Value = value;
        }

        partial void Init();
    }
}
