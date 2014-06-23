using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.Extensions
{
    public static class ListHelper
    {
        public static bool ContainsIgnoreCase(IEnumerable<string> list, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return list.Contains(value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
