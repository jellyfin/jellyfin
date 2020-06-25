using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity referencing an activity log entry.
    /// </summary>
    public partial class ActivityLog : ISavingChanges
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLog"/> class.
        /// Public constructor with required data.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="userId">The user id.</param>
        public ActivityLog(string name, string type, Guid userId)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentNullException(nameof(type));
            }

            this.Name = name;
            this.Type = type;
            this.UserId = userId;
            this.DateCreated = DateTime.UtcNow;
            this.LogSeverity = LogLevel.Trace;

            Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLog"/> class.
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected ActivityLog()
        {
            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        /// <param name="userId">The user's id.</param>
        /// <returns>The new <see cref="ActivityLog"/> instance.</returns>
        public static ActivityLog Create(string name, string type, Guid userId)
        {
            return new ActivityLog(name, type, userId);
        }

        /*************************************************************************
         * Properties
         *************************************************************************/

        /// <summary>
        /// Gets or sets the identity of this instance.
        /// This is the key in the backing database.
        /// </summary>
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        /// <summary>
        /// Gets or sets the name.
        /// Required, Max length = 512.
        /// </summary>
        [Required]
        [MaxLength(512)]
        [StringLength(512)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the overview.
        /// Max length = 512.
        /// </summary>
        [MaxLength(512)]
        [StringLength(512)]
        public string Overview { get; set; }

        /// <summary>
        /// Gets or sets the short overview.
        /// Max length = 512.
        /// </summary>
        [MaxLength(512)]
        [StringLength(512)]
        public string ShortOverview { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// Required, Max length = 256.
        /// </summary>
        [Required]
        [MaxLength(256)]
        [StringLength(256)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// Required.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the item id.
        /// Max length = 256.
        /// </summary>
        [MaxLength(256)]
        [StringLength(256)]
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the date created. This should be in UTC.
        /// Required.
        /// </summary>
        [Required]
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the log severity. Default is <see cref="LogLevel.Trace"/>.
        /// Required.
        /// </summary>
        [Required]
        public LogLevel LogSeverity { get; set; }

        /// <summary>
        /// Gets or sets the row version.
        /// Required, ConcurrencyToken.
        /// </summary>
        [ConcurrencyCheck]
        [Required]
        public uint RowVersion { get; set; }

        partial void Init();

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
