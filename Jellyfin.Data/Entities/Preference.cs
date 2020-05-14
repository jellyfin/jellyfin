using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class Preference
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected Preference()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static Preference CreatePreferenceUnsafe()
        {
            return new Preference();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="value"></param>
        /// <param name="_user0"></param>
        /// <param name="_group1"></param>
        public Preference(Enums.PreferenceKind kind, string value, User _user0, Group _group1)
        {
            this.Kind = kind;

            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
            this.Value = value;

            if (_user0 == null) throw new ArgumentNullException(nameof(_user0));
            _user0.Preferences.Add(this);

            if (_group1 == null) throw new ArgumentNullException(nameof(_group1));
            _group1.Preferences.Add(this);


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="value"></param>
        /// <param name="_user0"></param>
        /// <param name="_group1"></param>
        public static Preference Create(Enums.PreferenceKind kind, string value, User _user0, Group _group1)
        {
            return new Preference(kind, value, _user0, _group1);
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
        /// Required
        /// </summary>
        [Required]
        public Enums.PreferenceKind Kind { get; set; }

        /// <summary>
        /// Required, Max length = 65535
        /// </summary>
        [Required]
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Value { get; set; }

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

    }
}

