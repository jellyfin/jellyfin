using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities
{
    public partial class User
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected User()
        {
            Groups = new HashSet<Group>();
            Permissions = new HashSet<Permission>();
            ProviderMappings = new HashSet<ProviderMapping>();
            Preferences = new HashSet<Preference>();

            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static User CreateUserUnsafe()
        {
            return new User();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="username"></param>
        /// <param name="mustupdatepassword"></param>
        /// <param name="audiolanguagepreference"></param>
        /// <param name="authenticationproviderid"></param>
        /// <param name="invalidloginattemptcount"></param>
        /// <param name="subtitlemode"></param>
        /// <param name="playdefaultaudiotrack"></param>
        public User(string username, bool mustupdatepassword, string audiolanguagepreference, string authenticationproviderid, int invalidloginattemptcount, string subtitlemode, bool playdefaultaudiotrack)
        {
            if (string.IsNullOrEmpty(username)) throw new ArgumentNullException(nameof(username));
            this.Username = username;

            this.MustUpdatePassword = mustupdatepassword;

            if (string.IsNullOrEmpty(audiolanguagepreference)) throw new ArgumentNullException(nameof(audiolanguagepreference));
            this.AudioLanguagePreference = audiolanguagepreference;

            if (string.IsNullOrEmpty(authenticationproviderid)) throw new ArgumentNullException(nameof(authenticationproviderid));
            this.AuthenticationProviderId = authenticationproviderid;

            this.InvalidLoginAttemptCount = invalidloginattemptcount;

            if (string.IsNullOrEmpty(subtitlemode)) throw new ArgumentNullException(nameof(subtitlemode));
            this.SubtitleMode = subtitlemode;

            this.PlayDefaultAudioTrack = playdefaultaudiotrack;

            this.Groups = new HashSet<Group>();
            this.Permissions = new HashSet<Permission>();
            this.ProviderMappings = new HashSet<ProviderMapping>();
            this.Preferences = new HashSet<Preference>();

            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="username"></param>
        /// <param name="mustupdatepassword"></param>
        /// <param name="audiolanguagepreference"></param>
        /// <param name="authenticationproviderid"></param>
        /// <param name="invalidloginattemptcount"></param>
        /// <param name="subtitlemode"></param>
        /// <param name="playdefaultaudiotrack"></param>
        public static User Create(string username, bool mustupdatepassword, string audiolanguagepreference, string authenticationproviderid, int invalidloginattemptcount, string subtitlemode, bool playdefaultaudiotrack)
        {
            return new User(username, mustupdatepassword, audiolanguagepreference, authenticationproviderid, invalidloginattemptcount, subtitlemode, playdefaultaudiotrack);
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
        public string Username { get; set; }

        /// <summary>
        /// Max length = 65535
        /// </summary>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Password { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public bool MustUpdatePassword { get; set; }

        /// <summary>
        /// Required, Max length = 255
        /// </summary>
        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string AudioLanguagePreference { get; set; }

        /// <summary>
        /// Required, Max length = 255
        /// </summary>
        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string AuthenticationProviderId { get; set; }

        /// <summary>
        /// Max length = 65535
        /// </summary>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string GroupedFolders { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public int InvalidLoginAttemptCount { get; set; }

        /// <summary>
        /// Max length = 65535
        /// </summary>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string LatestItemExcludes { get; set; }

        public int? LoginAttemptsBeforeLockout { get; set; }

        /// <summary>
        /// Max length = 65535
        /// </summary>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string MyMediaExcludes { get; set; }

        /// <summary>
        /// Max length = 65535
        /// </summary>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string OrderedViews { get; set; }

        /// <summary>
        /// Required, Max length = 255
        /// </summary>
        [Required]
        [MaxLength(255)]
        [StringLength(255)]
        public string SubtitleMode { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public bool PlayDefaultAudioTrack { get; set; }

        /// <summary>
        /// Max length = 255
        /// </summary>
        [MaxLength(255)]
        [StringLength(255)]
        public string SubtitleLanguagePrefernce { get; set; }

        public bool? DisplayMissingEpisodes { get; set; }

        public bool? DisplayCollectionsView { get; set; }

        public bool? HidePlayedInLatest { get; set; }

        public bool? RememberAudioSelections { get; set; }

        public bool? RememberSubtitleSelections { get; set; }

        public bool? EnableNextEpisodeAutoPlay { get; set; }

        public bool? EnableUserPreferenceAccess { get; set; }

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
        [ForeignKey("Group_Groups_Id")]
        public virtual ICollection<Group> Groups { get; protected set; }

        [ForeignKey("Permission_Permissions_Id")]
        public virtual ICollection<Permission> Permissions { get; protected set; }

        [ForeignKey("ProviderMapping_ProviderMappings_Id")]
        public virtual ICollection<ProviderMapping> ProviderMappings { get; protected set; }

        [ForeignKey("Preference_Preferences_Id")]
        public virtual ICollection<Preference> Preferences { get; protected set; }

    }
}

