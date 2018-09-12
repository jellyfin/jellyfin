using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Emby.Naming.Video
{
    /// <summary>
    /// http://kodi.wiki/view/Advancedsettings.xml#video
    /// </summary>
    public class CleanStringParser
    {
        public CleanStringResult Clean(string name, IEnumerable<Regex> expressions)
        {
            var hasChanged = false;

            foreach (var exp in expressions)
            {
                var result = Clean(name, exp);

                if (!string.IsNullOrEmpty(result.Name))
                {
                    name = result.Name;
                    hasChanged = hasChanged || result.HasChanged;
                }
            }

            return new CleanStringResult
            {
                Name = name,
                HasChanged = hasChanged
            };
        }

        private CleanStringResult Clean(string name, Regex expression)
        {
            var result = new CleanStringResult();

            var match = expression.Match(name);

            if (match.Success)
            {
                result.HasChanged = true;
                name = name.Substring(0, match.Index);
            }

            result.Name = name;
            return result;
        }
    }
}
