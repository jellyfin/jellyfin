#pragma warning disable CS1591

using System;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Common.Updates
{
    public class InstallationEventArgs : EventArgs
    {
        public InstallationEventArgs(InstallationInfo installationInfo)
        {
            InstallationInfo = installationInfo;
        }

        public InstallationInfo InstallationInfo { get; set; }

        public VersionInfo? VersionInfo { get; set; }
    }
}
