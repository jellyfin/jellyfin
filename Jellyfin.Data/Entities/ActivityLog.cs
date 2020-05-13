using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Data.Entities
{
    public partial class ActivityLog
    {
        partial void Init();

        /// <summary>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected ActivityLog()
        {
            Init();
        }

        /// <summary>
        /// Replaces default constructor, since it's protected. Caller assumes responsibility for setting all required values before saving.
        /// </summary>
        public static ActivityLog CreateActivityLogUnsafe()
        {
            return new ActivityLog();
        }

        /// <summary>
        /// Public constructor with required data
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="userId"></param>
        /// <param name="datecreated"></param>
        /// <param name="logSeverity"></param>
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
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="userId"></param>
        /// <param name="datecreated"></param>
        /// <param name="logseverity"></param>
        public static ActivityLog Create(string name, string type, Guid userId)
        {
            return new ActivityLog(name, type, userId);
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
        /// Required, Max length = 512
        /// </summary>
        [Required]
        [MaxLength(512)]
        [StringLength(512)]
        public string Name { get; set; }

        /// <summary>
        /// Max length = 512
        /// </summary>
        [MaxLength(512)]
        [StringLength(512)]
        public string Overview { get; set; }

        /// <summary>
        /// Max length = 512
        /// </summary>
        [MaxLength(512)]
        [StringLength(512)]
        public string ShortOverview { get; set; }

        /// <summary>
        /// Required, Max length = 256
        /// </summary>
        [Required]
        [MaxLength(256)]
        [StringLength(256)]
        public string Type { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Max length = 256
        /// </summary>
        [MaxLength(256)]
        [StringLength(256)]
        public string ItemId { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Required
        /// </summary>
        [Required]
        public LogLevel LogSeverity { get; set; }

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
    }
}

