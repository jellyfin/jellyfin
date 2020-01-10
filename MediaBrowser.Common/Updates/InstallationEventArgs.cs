#pragma warning disable CS1591
#pragma warning disable SA1600

using MediaBrowser.Model.Updates;

namespace MediaBrowser.Common.Updates
{
    public class InstallationEventArgs
    {
        public InstallationInfo InstallationInfo { get; set; }

        public PackageVersionInfo PackageVersionInfo { get; set; }
    }
}
