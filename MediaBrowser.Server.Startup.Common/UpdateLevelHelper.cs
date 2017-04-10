using System;
using System.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Server.Startup.Common
{
    public static class UpdateLevelHelper
    {
        public static PackageVersionClass GetSystemUpdateLevel(IConfigurationManager config)
        {
            return config.CommonConfiguration.SystemUpdateLevel;
            //var configuredValue = ConfigurationManager.AppSettings["SystemUpdateLevel"];

            //if (string.Equals(configuredValue, "Beta", StringComparison.OrdinalIgnoreCase))
            //{
            //    return PackageVersionClass.Beta;
            //}
            //if (string.Equals(configuredValue, "Dev", StringComparison.OrdinalIgnoreCase))
            //{
            //    return PackageVersionClass.Dev;
            //}

            //return PackageVersionClass.Release;
        }
    }
}
