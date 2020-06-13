#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Services
{
    // Remove this garbage class, it's just a bastard copy of NameValueCollection
    public class QueryParamCollection : List<NameValuePair>
    {
        public QueryParamCollection()
        {
        }

        private static StringComparison GetStringComparison()
        {
            return StringComparison.OrdinalIgnoreCase;
        }

        /// <summary>
        /// Adds a new query parameter.
        /// </summary>
        public void Add(string key, string value)
        {
            Add(new NameValuePair(key, value));
        }

        private void Set(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                var parameters = GetItems(key);

                foreach (var p in parameters)
                {
                    Remove(p);
                }

                return;
            }

            foreach (var pair in this)
            {
                var stringComparison = GetStringComparison();

                if (string.Equals(key, pair.Name, stringComparison))
                {
                    pair.Value = value;
                    return;
                }
            }

            Add(key, value);
        }

        private string Get(string name)
        {
            var stringComparison = GetStringComparison();

            foreach (var pair in this)
            {
                if (string.Equals(pair.Name, name, stringComparison))
                {
                    return pair.Value;
                }
            }

            return null;
        }

        private List<NameValuePair> GetItems(string name)
        {
            var stringComparison = GetStringComparison();

            var list = new List<NameValuePair>();

            foreach (var pair in this)
            {
                if (string.Equals(pair.Name, name, stringComparison))
                {
                    list.Add(pair);
                }
            }

            return list;
        }

        public virtual List<string> GetValues(string name)
        {
            var stringComparison = GetStringComparison();

            var list = new List<string>();

            foreach (var pair in this)
            {
                if (string.Equals(pair.Name, name, stringComparison))
                {
                    list.Add(pair.Value);
                }
            }

            return list;
        }

        public IEnumerable<string> Keys
        {
            get
            {
                var keys = new string[this.Count];

                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i] = this[i].Name;
                }

                return keys;
            }
        }

        /// <summary>
        /// Gets or sets a query parameter value by name. A query may contain multiple values of the same name
        /// (i.e. "x=1&amp;x=2"), in which case the value is an array, which works for both getting and setting.
        /// </summary>
        /// <param name="name">The query parameter name</param>
        /// <returns>The query parameter value or array of values</returns>
        public string this[string name]
        {
            get => Get(name);
            set => Set(name, value);
        }

        private string GetQueryStringValue(NameValuePair pair)
        {
            return pair.Name + "=" + pair.Value;
        }

        public override string ToString()
        {
            var vals = this.Select(GetQueryStringValue).ToArray();

            return string.Join("&", vals);
        }
    }
}
