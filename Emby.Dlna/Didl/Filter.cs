#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using MediaBrowser.Model.Extensions;

namespace Emby.Dlna.Didl
{
    public class Filter
    {
        private readonly string[] _fields;
        private readonly bool _all;

        public Filter()
            : this("*")
        {

        }

        public Filter(string filter)
        {
            _all = string.Equals(filter, "*", StringComparison.OrdinalIgnoreCase);

            _fields = (filter ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool Contains(string field)
        {
            // Don't bother with this. Some clients (media monkey) use the filter and then don't display very well when very little data comes back.
            return true;
            //return _all || ListHelper.ContainsIgnoreCase(_fields, field);
        }
    }
}
