using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Jellyfin.Data.Entities
{
    [Table("ActivityLog")]
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
        /// <param name="userid"></param>
        /// <param name="datecreated"></param>
        /// <param name="logseverity"></param>
        public ActivityLog(string name, string type, Guid userid, DateTime datecreated, Microsoft.Extensions.Logging.LogLevel logseverity)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            this.Name = name;

            if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));
            this.Type = type;

            this.UserId = userid;

            this.DateCreated = datecreated;

            this.LogSeverity = logseverity;


            Init();
        }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="userid"></param>
        /// <param name="datecreated"></param>
        /// <param name="logseverity"></param>
        public static ActivityLog Create(string name, string type, Guid userid, DateTime datecreated, Microsoft.Extensions.Logging.LogLevel logseverity)
        {
            return new ActivityLog(name, type, userid, datecreated, logseverity);
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
        public Microsoft.Extensions.Logging.LogLevel LogSeverity { get; set; }

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

    }
}

