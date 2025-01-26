using System;
using System.ComponentModel.DataAnnotations.Schema;
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
        /// Gets the id of this instance.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [XmlIgnore]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets the id of the associated user.
        /// </summary>
        [XmlIgnore]
        public Guid UserId { get; private set; }

        /// <summary>
        /// Gets or sets the day of week.
        /// </summary>
        /// <value>The day of week.</value>
        public DynamicDayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets the start hour.
        /// </summary>
        /// <value>The start hour.</value>
        public double StartHour { get; set; }

        /// <summary>
        /// Gets or sets the end hour.
        /// </summary>
        /// <value>The end hour.</value>
        public double EndHour { get; set; }
    }
}
