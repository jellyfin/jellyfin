#pragma warning disable CS1591

using MediaBrowser.Model.Updates;

namespace MediaBrowser.Common.Updates
{
    public class InstallationEventArgs
    {
        public InstallationInfo InstallationInfo { get; set; }

        public VersionInfo VersionInfo { get; set; }
    }
}
