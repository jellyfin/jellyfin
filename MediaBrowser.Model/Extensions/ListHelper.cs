#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Extensions
{
    // TODO: @bond remove
    public static class ListHelper
    {
        public static bool ContainsIgnoreCase(string[] list, string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            foreach (var item in list)
            {
                if (string.Equals(item, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
