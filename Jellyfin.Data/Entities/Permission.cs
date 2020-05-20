using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    public partial class Permission : ISavingChanges
    {
        partial void Init();

        /// <summary>
        /// Initializes a new instance of the <see cref="Permission"/> class.
        /// Public constructor with required data
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="value"></param>
        /// <param name="holderId"></param>
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

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="value"></param>
        /// <param name="holderId"></param>
        public static Permission Create(PermissionKind kind, bool value)
        {
            return new Permission(kind, value);
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
        protected PermissionKind _Kind;
        /// <summary>
        /// When provided in a partial class, allows value of Kind to be changed before setting.
        /// </summary>
        partial void SetKind(PermissionKind oldValue, ref PermissionKind newValue);
        /// <summary>
        /// When provided in a partial class, allows value of Kind to be changed before returning.
        /// </summary>
        partial void GetKind(ref PermissionKind result);

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public PermissionKind Kind
        {
            get
            {
                PermissionKind value = _Kind;
                GetKind(ref value);
                return _Kind = value;
            }

            set
            {
                PermissionKind oldValue = _Kind;
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
        /// Required, ConcurrencyToken.
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

