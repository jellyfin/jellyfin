using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class PersonRole
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected PersonRole()
        {
            // NOTE: This class has one-to-one associations with PersonRole.
            // One-to-one associations are not validated in constructors since this causes a scenario where each one must be constructed before the other.

            Sources = new HashSet<MetadataProviderId>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static PersonRole CreatePersonRoleUnsafe()
        {
            return new PersonRole();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="type"></param>
        /// <param name="_metadata0"></param>
        public PersonRole(Enums.PersonRoleType type, Metadata _metadata0)
        {
            // NOTE: This class has one-to-one associations with PersonRole.
            // One-to-one associations are not validated in constructors since this causes a scenario where each one must be constructed before the other.

            this.Type = type;

            if (_metadata0 == null) throw new ArgumentNullException(nameof(_metadata0));
            _metadata0.PersonRoles.Add(this);

            this.Sources = new HashSet<MetadataProviderId>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="_metadata0"></param>
        public static PersonRole Create(Enums.PersonRoleType type, Metadata _metadata0)
        {
            return new PersonRole(type, _metadata0);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Backing field for Id
        /// </summary>
        internal int _Id;
        /// <summary>
        /// When provided in a partial class, allows value of Id to be changed before setting.
        /// </summary>
        partial void SetId(int oldValue, ref int newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Id to be changed before returning.
        /// </summary>
        partial void GetId(ref int result);

        /// <summary>
        /// Identity, Indexed, Required
        /// </summary>
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id
        {
            get
            {
                int value = _Id;
                GetId(ref value);
                return (_Id = value);
            }
            protected set
            {
                int oldValue = _Id;
                SetId(oldValue, ref value);
                if (oldValue != value)
                {
                    _Id = value;
                }
            }
        }

        /// <summary>
        /// Backing field for Role
        /// </summary>
        protected string _Role;
        /// <summary>
        /// When provided in a partial class, allows value of Role to be changed before setting.
        /// </summary>
        partial void SetRole(string oldValue, ref string newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Role to be changed before returning.
        /// </summary>
        partial void GetRole(ref string result);

        /// <summary>
        /// Max length = 1024
        /// </summary>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Role
        {
            get
            {
                string value = _Role;
                GetRole(ref value);
                return (_Role = value);
            }
            set
            {
                string oldValue = _Role;
                SetRole(oldValue, ref value);
                if (oldValue != value)
                {
                    _Role = value;
                }
            }
        }

        /// <summary>
        /// Backing field for Type
        /// </summary>
        protected Enums.PersonRoleType _Type;
        /// <summary>
        /// When provided in a partial class, allows value of Type to be changed before setting.
        /// </summary>
        partial void SetType(Enums.PersonRoleType oldValue, ref Enums.PersonRoleType newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Type to be changed before returning.
        /// </summary>
        partial void GetType(ref Enums.PersonRoleType result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public Enums.PersonRoleType Type
        {
            get
            {
                Enums.PersonRoleType value = _Type;
                GetType(ref value);
                return (_Type = value);
            }
            set
            {
                Enums.PersonRoleType oldValue = _Type;
                SetType(oldValue, ref value);
                if (oldValue != value)
                {
                    _Type = value;
                }
            }
        }

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

        /// <summary>
        /// Required
        /// </summary>
        [ForeignKey("Person_Id")]

        public virtual Person Person { get; set; }

        [ForeignKey("Artwork_Artwork_Id")]
        public virtual Artwork Artwork { get; set; }

        [ForeignKey("MetadataProviderId_Sources_Id")]
        public virtual ICollection<MetadataProviderId> Sources { get; protected set; }

    }
}

