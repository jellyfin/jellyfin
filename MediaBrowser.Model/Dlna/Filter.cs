using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.Dlna
{
    public class Filter
    {
        private readonly List<string> _fields;
        private readonly bool _all;

        public Filter()
            : this("*")
        {

        }

        public Filter(string filter)
        {
            _all = string.Equals(filter, "*", StringComparison.OrdinalIgnoreCase);

            _fields = (filter ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

        public bool Contains(string field)
        {
            return _all || _fields.Contains(field, StringComparer.OrdinalIgnoreCase);
        }
    }
}
