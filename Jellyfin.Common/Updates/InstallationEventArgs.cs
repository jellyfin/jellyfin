using Jellyfin.Model.Updates;

namespace Jellyfin.Common.Updates
{
    public class InstallationEventArgs
    {
        public InstallationInfo InstallationInfo { get; set; }

        public PackageVersionInfo PackageVersionInfo { get; set; }
    }
}
