using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.Extensions
{
    public static class ListHelper
    {
        public static bool ContainsIgnoreCase(List<string> list, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return list.Contains(value, StringComparer.OrdinalIgnoreCase);
        }
        public static bool ContainsIgnoreCase(string[] list, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return list.Contains(value, StringComparer.OrdinalIgnoreCase);
        }

        public static bool ContainsAnyIgnoreCase(string[] list, string[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            foreach (string val in values)
            {
                if (ContainsIgnoreCase(list, val))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
