using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Jellyfin.Data.Entities
{
    public partial class Permission
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Permission()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Permission CreatePermissionUnsafe()
        {
            return new Permission();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="value"></param>
        /// <param name="_user0"></param>
        /// <param name="_group1"></param>
        public Permission(Enums.PermissionKind kind, bool value, User _user0, Group _group1)
        {
            this.Kind = kind;

            this.Value = value;

            if (_user0 == null) throw new ArgumentNullException(nameof(_user0));
            _user0.Permissions.Add(this);

            if (_group1 == null) throw new ArgumentNullException(nameof(_group1));
            _group1.GroupPermissions.Add(this);


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="value"></param>
        /// <param name="_user0"></param>
        /// <param name="_group1"></param>
        public static Permission Create(Enums.PermissionKind kind, bool value, User _user0, Group _group1)
        {
            return new Permission(kind, value, _user0, _group1);
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
        /// Backing field for Kind
        /// </summary>
        protected Enums.PermissionKind _Kind;
        /// <summary>
        /// When provided in a partial class, allows value of Kind to be changed before setting.
        /// </summary>
        partial void SetKind(Enums.PermissionKind oldValue, ref Enums.PermissionKind newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Kind to be changed before returning.
        /// </summary>
        partial void GetKind(ref Enums.PermissionKind result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public Enums.PermissionKind Kind
        {
            get
            {
                Enums.PermissionKind value = _Kind;
                GetKind(ref value);
                return (_Kind = value);
            }
            set
            {
                Enums.PermissionKind oldValue = _Kind;
                SetKind(oldValue, ref value);
                if (oldValue != value)
                {
                    _Kind = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public bool Value { get; set; }

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

        public virtual event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}

