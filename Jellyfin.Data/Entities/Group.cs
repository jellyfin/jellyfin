using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    public partial class Group : IHasPermissions, ISavingChanges
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Group()
        {
            Permissions = new HashSet<Permission>();
            ProviderMappings = new HashSet<ProviderMapping>();
            Preferences = new HashSet<Preference>();

            Init();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="name"></param>
        /// <param name="user"></param>
        public Group(string name, User user)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            this.Name = name;
            user.Groups.Add(this);

            this.Permissions = new HashSet<Permission>();
            this.ProviderMappings = new HashSet<ProviderMapping>();
            this.Preferences = new HashSet<Preference>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="_user0"></param>
        public static Group Create(string name, User user)
        {
            return new Group(name, user);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Identity, Indexed, Required
        /// </summary>
        [Key]
        [Required]
        public Guid Id { get; protected set; }

        /// <summary>
        /// Required, Max length = 255
        /// </summary>
        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string Name { get; set; }

        /// <summary>
        /// Required, ConcurrenyToken
        /// </summary>
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

        public bool HasPermission(PermissionKind kind)
        {
            return Permissions.First(p => p.Kind == kind).Value;
        }

        public void SetPermission(PermissionKind kind, bool value)
        {
            Permissions.First(p => p.Kind == kind).Value = value;
        }
    }
}
