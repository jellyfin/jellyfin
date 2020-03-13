#pragma warning disable CS1591

using System;

namespace MediaBrowser.Common.Updates
{
    public class InstallationFailedEventArgs : InstallationEventArgs
    {
        public Exception Exception { get; set; }
    }
}
