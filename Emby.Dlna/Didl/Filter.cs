using System;

namespace Emby.Dlna.Didl
{
    /// <summary>
    /// Defines the <see cref="Filter" />.
    /// </summary>
    public class Filter
    {
        private readonly string[] _fields;
        private readonly bool _all;

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// </summary>
        public Filter()
            : this("*")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Filter"/> class.
        /// </summary>
        /// <param name="filter">The filter.</param>
        public Filter(string filter)
        {
            _all = string.Equals(filter, "*", StringComparison.OrdinalIgnoreCase);

            _fields = (filter ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Returns true if the filter contains the field, taking into consideration wildcards.
        /// </summary>
        /// <param name="field">The field to look for.</param>
        /// <returns>The result of the operation.</returns>
        public bool Contains(string field)
        {
            return _all || Array.Exists(_fields, x => x.Equals(field, StringComparison.OrdinalIgnoreCase));
        }
    }
}
