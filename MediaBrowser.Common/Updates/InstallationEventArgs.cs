using MediaBrowser.Model.Updates;

namespace MediaBrowser.Common.Updates
{
    public class InstallationEventArgs
    {
        public InstallationInfo InstallationInfo { get; set; }

        public PackageVersionInfo PackageVersionInfo { get; set; }
    }
}
