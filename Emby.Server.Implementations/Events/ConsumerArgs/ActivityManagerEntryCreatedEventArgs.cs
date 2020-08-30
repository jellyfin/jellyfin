using Jellyfin.Data.Events;
using MediaBrowser.Model.Activity;

namespace Emby.Server.Implementations.Events.ConsumerArgs
{
    /// <summary>
    /// An event that occurs when an activity entry is created.
    /// </summary>
    public class ActivityManagerEntryCreatedEventArgs : GenericEventArgs<ActivityLogEntry>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityManagerEntryCreatedEventArgs"/> class.
        /// </summary>
        /// <param name="arg">The activity log entry.</param>
        public ActivityManagerEntryCreatedEventArgs(ActivityLogEntry arg) : base(arg)
        {
        }
    }
}
