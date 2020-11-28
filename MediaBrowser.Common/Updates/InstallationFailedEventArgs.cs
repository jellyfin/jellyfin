#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Common.Updates
{
    public class InstallationFailedEventArgs : InstallationEventArgs
    {
        public InstallationFailedEventArgs(Exception exception, InstallationInfo installationInfo)
            : base(installationInfo)
        {
            Exception = exception;
        }

        public Exception Exception { get; set; }
    }
}
