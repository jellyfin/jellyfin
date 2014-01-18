using MediaBrowser.IsoMounter;
using System.Collections.Generic;
using System.Reflection;

namespace MediaBrowser.ServerApplication.Native
{
    /// <summary>
    /// Class Assemblies
    /// </summary>
    public static class Assemblies
    {
        /// <summary>
        /// Gets the assemblies with parts.
        /// </summary>
        /// <returns>List{Assembly}.</returns>
        public static List<Assembly> GetAssembliesWithParts()
        {
            var list = new List<Assembly>();

            list.Add(typeof(LinuxIsoManager).Assembly);

            return list;
        }
    }
}
