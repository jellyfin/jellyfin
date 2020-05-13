using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities
{
    public class AccessSchedule
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccessSchedule"/> class.
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </summary>
        protected AccessSchedule()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessSchedule"/> class.
        /// </summary>
        /// <param name="dayOfWeek">The day of the week.</param>
        /// <param name="startHour">The start hour.</param>
        /// <param name="endHour">The end hour.</param>
        public AccessSchedule(DynamicDayOfWeek dayOfWeek, double startHour, double endHour)
        {
            DayOfWeek = dayOfWeek;
            StartHour = startHour;
            EndHour = endHour;
        }

        /// <summary>
        /// Factory method
        /// </summary>
        /// <param name="dayOfWeek">The day of the week.</param>
        /// <param name="startHour">The start hour.</param>
        /// <param name="endHour">The end hour.</param>
        /// <returns>The newly created instance.</returns>
        public static AccessSchedule CreateInstance(DynamicDayOfWeek dayOfWeek, double startHour, double endHour)
        {
            return new AccessSchedule(dayOfWeek, startHour, endHour);
        }

        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

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
    }
}
