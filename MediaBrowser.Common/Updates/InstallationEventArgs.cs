using MediaBrowser.Model.Updates;
using System;

namespace MediaBrowser.Common.Updates
{
    public class InstallationEventArgs
    {
        public InstallationInfo InstallationInfo { get; set; }

        public PackageVersionInfo PackageVersionInfo { get; set; }
    }

    public class InstallationFailedEventArgs : InstallationEventArgs
    {
        public Exception Exception { get; set; }
    }
}
