using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing a user's access schedule.
    /// </summary>
    public class AccessSchedule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccessSchedule"/> class.
        /// </summary>
        /// <param name="dayOfWeek">The day of the week.</param>
        /// <param name="startHour">The start hour.</param>
        /// <param name="endHour">The end hour.</param>
        /// <param name="userId">The associated user's id.</param>
        public AccessSchedule(DynamicDayOfWeek dayOfWeek, double startHour, double endHour, Guid userId)
        {
            UserId = userId;
            DayOfWeek = dayOfWeek;
            StartHour = startHour;
            EndHour = endHour;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessSchedule"/> class.
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected AccessSchedule()
        {
        }

        /// <summary>
        /// Gets or sets the id of this instance.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [XmlIgnore]
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        /// <summary>
        /// Gets or sets the id of the associated user.
        /// </summary>
        [XmlIgnore]
        [Required]
        public Guid UserId { get; protected set; }

        /// <summary>
        /// Gets or sets the day of week.
        /// </summary>
        /// <value>The day of week.</value>
        [Required]
        public DynamicDayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the start hour.
        /// </summary>
        /// <value>The start hour.</value>
        [Required]
        public double StartHour { get; set; }

        /// <summary>
        /// Gets or sets the end hour.
        /// </summary>
        /// <value>The end hour.</value>
        [Required]
        public double EndHour { get; set; }

        /// <summary>
        /// Static create function (for use in LINQ queries, etc.)
        /// </summary>
        /// <param name="dayOfWeek">The day of the week.</param>
        /// <param name="startHour">The start hour.</param>
        /// <param name="endHour">The end hour.</param>
        /// <param name="userId">The associated user's id.</param>
        /// <returns>The newly created instance.</returns>
        public static AccessSchedule Create(DynamicDayOfWeek dayOfWeek, double startHour, double endHour, Guid userId)
        {
            return new AccessSchedule(dayOfWeek, startHour, endHour, userId);
        }
    }
}
