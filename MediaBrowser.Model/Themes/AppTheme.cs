using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Themes
{
    public class AppTheme
    {
        public string ApplicationName { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> Options { get; set; }

        public List<ThemeImage> Images { get; set; }

        public AppTheme()
        {
            Options = new Dictionary<string, string>(StringComparer.Ordinal);

            Images = new List<ThemeImage>();
        }
    }

    public class AppThemeInfo
    {
        public string ApplicationName { get; set; }
        
        public string Name { get; set; }
    }
}
