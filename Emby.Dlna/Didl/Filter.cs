#pragma warning disable CS1591

using System;

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
            _fields = filter.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        public bool Contains(string field)
        {
            return _all || Array.Exists(_fields, x => x.Equals(field, StringComparison.OrdinalIgnoreCase));
        }
    }
}
