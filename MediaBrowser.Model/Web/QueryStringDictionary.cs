using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.Web
{
    /// <summary>
    /// Class QueryStringDictionary
    /// </summary>
    public class QueryStringDictionary : Dictionary<string, string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryStringDictionary" /> class.
        /// </summary>
        public QueryStringDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void Add(string name, int value)
        {
            Add(name, value.ToString());
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void Add(string name, long value)
        {
            Add(name, value.ToString());
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void Add(string name, double value)
        {
            Add(name, value.ToString());
        }

        /// <summary>
        /// Adds if not null or empty.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddIfNotNullOrEmpty(string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                Add(name, value);
            }
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddIfNotNull(string name, int? value)
        {
            if (value.HasValue)
            {
                Add(name, value.Value);
            }
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddIfNotNull(string name, double? value)
        {
            if (value.HasValue)
            {
                Add(name, value.Value);
            }
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddIfNotNull(string name, long? value)
        {
            if (value.HasValue)
            {
                Add(name, value.Value);
            }
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public void Add(string name, bool value)
        {
            Add(name, value.ToString());
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        public void AddIfNotNull(string name, bool? value)
        {
            if (value.HasValue)
            {
                Add(name, value.Value);
            }
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public void Add(string name, Guid value)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentNullException("value");
            }

            Add(name, value.ToString());
        }

        /// <summary>
        /// Adds if not empty.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddIfNotEmpty(string name, Guid value)
        {
            if (value != Guid.Empty)
            {
                Add(name, value);
            }

            Add(name, value);
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddIfNotNull(string name, Guid? value)
        {
            if (value.HasValue)
            {
                Add(name, value.Value);
            }
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public void Add(string name, IEnumerable<int> value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            Add(name, string.Join(",", value.Select(v => v.ToString()).ToArray()));
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddIfNotNull(string name, IEnumerable<int> value)
        {
            if (value != null)
            {
                Add(name, value);
            }
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public void Add(string name, IEnumerable<string> value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var paramValue = string.Join(",", value.ToArray());

            Add(name, paramValue);
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddIfNotNull(string name, IEnumerable<string> value)
        {
            if (value != null)
            {
                Add(name, value);
            }
        }

        /// <summary>
        /// Adds the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public void Add(string name, IEnumerable<string> value, string delimiter)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            var paramValue = string.Join(delimiter, value.ToArray());

            Add(name, paramValue);
        }

        /// <summary>
        /// Adds if not null.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <param name="delimiter">The delimiter.</param>
        public void AddIfNotNull(string name, IEnumerable<string> value, string delimiter)
        {
            if (value != null)
            {
                Add(name, value, delimiter);
            }
        }

        /// <summary>
        /// Gets the query string.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetQueryString()
        {
            var queryParams = this.Select(i => string.Format("{0}={1}", i.Key, GetEncodedValue(i.Value))).ToArray();

            return string.Join("&", queryParams);
        }

        /// <summary>
        /// Gets the encoded value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
        private string GetEncodedValue(string value)
        {
            return value;
        }

        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <param name="prefix">The prefix.</param>
        /// <returns>System.String.</returns>
        public string GetUrl(string prefix)
        {
            var query = GetQueryString();

            if (string.IsNullOrEmpty(query))
            {
                return prefix;
            }

            return prefix + "?" + query;
        }
    }
}
