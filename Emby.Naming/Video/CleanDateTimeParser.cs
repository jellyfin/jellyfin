using Emby.Naming.Common;
using Emby.Naming.Video.DateTimeResolvers;

namespace Emby.Naming.Video
{
    /// <summary>
    /// <see href="http://kodi.wiki/view/Advancedsettings.xml#video" />.
    /// </summary>
    public static class CleanDateTimeParser
    {
        /// <summary>
        /// Attempts to clean the name.
        /// </summary>
        /// <param name="name">Name of video.</param>
        /// <param name="namingOptions">intance of NamingOptions.</param>
        /// <returns>Returns <see cref="CleanDateTimeResult"/> object.</returns>
        public static CleanDateTimeResult Clean(string name, NamingOptions namingOptions)
        {
            var resolver = new MovieDateTimeResolverComposite();

            var result = resolver.Resolve(name, namingOptions);

            if (string.IsNullOrEmpty(name) || result == null)
            {
                name = DateTimeResolverHelpers.TrimAfterFirstNonTitleOccurrence(name, namingOptions.NonTitleStringsRegexes);
                return new CleanDateTimeResult(name);
            }

            return result.Value;
        }
    }
}
