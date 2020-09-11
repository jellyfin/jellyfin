#pragma warning disable CS1591

using System.IO;
using System.Reflection;

namespace MediaBrowser.Controller.Plugins
{
    public interface ILocalizablePlugin
    {
        Stream GetDictionary(string culture);
    }

    public static class LocalizablePluginHelper
    {
        public static Stream GetDictionary(Assembly assembly, string manifestPrefix, string culture)
        {
            // Find all dictionaries using GetManifestResourceNames, start start with the prefix
            // Return the one for the culture if exists, otherwise return the default
            return null;
        }
    }
}
