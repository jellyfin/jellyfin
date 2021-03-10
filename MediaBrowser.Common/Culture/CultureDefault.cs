using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Culture
{
    /// <summary>
    /// Helper class for culture info across the codebase.
    /// </summary>
    public static class CultureDefault
    {
        /// <summary>
        /// US Culture Info.
        /// </summary>
        public static readonly CultureInfo UsCulture = CultureInfo.ReadOnly(new CultureInfo("en-US"));
    }
}
