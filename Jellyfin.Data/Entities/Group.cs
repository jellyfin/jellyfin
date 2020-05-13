using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Group
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Group()
        {
            GroupPermissions = new HashSet<Permission>();
            ProviderMappings = new HashSet<ProviderMapping>();
            Preferences = new HashSet<Preference>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Group CreateGroupUnsafe()
        {
            return new Group();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="name"></param>
        /// <param name="_user0"></param>
        public Group(string name, User _user0)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            this.Name = name;

            if (_user0 == null) throw new ArgumentNullException(nameof(_user0));
            _user0.Groups.Add(this);

            this.GroupPermissions = new HashSet<Permission>();
            this.ProviderMappings = new HashSet<ProviderMapping>();
            this.Preferences = new HashSet<Preference>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="_user0"></param>
        public static Group Create(string name, User _user0)
        {
            return new Group(name, _user0);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Identity, Indexed, Required
        /// </summary>
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

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
        public virtual ICollection<Permission> GroupPermissions { get; protected set; }

        [ForeignKey("ProviderMapping_ProviderMappings_Id")]
        public virtual ICollection<ProviderMapping> ProviderMappings { get; protected set; }

        [ForeignKey("Preference_Preferences_Id")]
        public virtual ICollection<Preference> Preferences { get; protected set; }

    }
}

