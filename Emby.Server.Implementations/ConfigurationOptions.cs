using System.Collections.Generic;
using Emby.Server.Implementations.IO;

namespace Emby.Server.Implementations
{
    public static class ConfigurationOptions
    {
        public static readonly Dictionary<string, string> Configuration = new Dictionary<string, string>
        {
            {"ManagedFileSystem:DefaultDirectory", null},
            {"ManagedFileSystem:EnableSeparateFileAndDirectoryQueries", "True"}
        };
    }
}
