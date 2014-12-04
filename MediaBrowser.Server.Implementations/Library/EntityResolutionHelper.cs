using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.IO;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Class EntityResolutionHelper
    /// </summary>
    public static class EntityResolutionHelper
    {
        /// <summary>
        /// Any folder named in this list will be ignored - can be added to at runtime for extensibility
        /// </summary>
        public static readonly List<string> IgnoreFolders = new List<string>
        {
                "metadata",
                "ps3_update",
                "ps3_vprm",
                "extrafanart",
                "extrathumbs",
                ".actors",
                ".wd_tv"

        };
    }
}
