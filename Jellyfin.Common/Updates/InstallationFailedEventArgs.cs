using System;

namespace Jellyfin.Common.Updates
{
    public class InstallationFailedEventArgs : InstallationEventArgs
    {
        public Exception Exception { get; set; }
    }
}
