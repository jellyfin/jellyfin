using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Services
{
    public class QueryParamCollection : List<NameValuePair>
    {
        public QueryParamCollection()
        {

        }

        public QueryParamCollection(IDictionary<string, string> headers)
        {
            foreach (var pair in headers)
            {
                Add(pair.Key, pair.Value);
            }
        }

        private StringComparison GetStringComparison()
        {
            return StringComparison.OrdinalIgnoreCase;
        }

        private StringComparer GetStringComparer()
        {
            return StringComparer.OrdinalIgnoreCase;
        }

        public string GetKey(int index)
        {
            return this[index].Name;
        }

        public string Get(int index)
        {
            return this[index].Value;
        }

        public virtual string[] GetValues(int index)
        {
            return new[] { Get(index) };
        }

        /// <summary>
        /// Adds a new query parameter.
        /// </summary>
        public virtual void Add(string key, string value)
        {
            Add(new NameValuePair(key, value));
        }

        public virtual void Set(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                var stringComparison = GetStringComparison();

                var parameters = this.Where(p => string.Equals(key, p.Name, stringComparison)).ToArray();

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

        /// <summary>
        /// True if the collection contains a query parameter with the given name.
        /// </summary>
        public bool ContainsKey(string name)
        {
            return this.Any(p => p.Name == name);
        }

        /// <summary>
        /// Removes all parameters of the given name.
        /// </summary>
        /// <returns>The number of parameters that were removed</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name" /> is null.</exception>
        public virtual int Remove(string name)
        {
            return RemoveAll(p => p.Name == name);
        }

        public string Get(string name)
        {
            var stringComparison = GetStringComparison();

            return this.Where(p => string.Equals(p.Name, name, stringComparison))
                .Select(p => p.Value)
                .FirstOrDefault();
        }

        public virtual string[] GetValues(string name)
        {
            var stringComparison = GetStringComparison();

            return this.Where(p => string.Equals(p.Name, name, stringComparison)).Select(p => p.Value).ToArray();
        }

        public Dictionary<string, string> ToDictionary()
        {
            var stringComparer = GetStringComparer();

            var headers = new Dictionary<string, string>(stringComparer);

            foreach (var pair in this)
            {
                headers[pair.Name] = pair.Value;
            }

            return headers;
        }

        public IEnumerable<string> Keys
        {
            get { return this.Select(i => i.Name); }
        }

        /// <summary>
        /// Gets or sets a query parameter value by name. A query may contain multiple values of the same name
        /// (i.e. "x=1&amp;x=2"), in which case the value is an array, which works for both getting and setting.
        /// </summary>
        /// <param name="name">The query parameter name</param>
        /// <returns>The query parameter value or array of values</returns>
        public string this[string name]
        {
            get { return Get(name); }
            set
            {
                Set(name, value);
                //var parameters = this.Where(p => p.Name == name).ToArray();
                //var values = new[] { value };

                //for (int i = 0; ; i++)
                //{
                //    if (i < parameters.Length && i < values.Length)
                //    {
                //        if (values[i] == null)
                //            Remove(parameters[i]);
                //        else if (values[i] is NameValuePair)
                //            this[IndexOf(parameters[i])] = (NameValuePair)values[i];
                //        else
                //            parameters[i].Value = values[i];
                //    }
                //    else if (i < parameters.Length)
                //        Remove(parameters[i]);
                //    else if (i < values.Length)
                //    {
                //        if (values[i] != null)
                //        {
                //            if (values[i] is NameValuePair)
                //                Add((NameValuePair)values[i]);
                //            else
                //                Add(name, values[i]);
                //        }
                //    }
                //    else
                //        break;
                //}
            }
        }

        private string GetQueryStringValue(NameValuePair pair)
        {
            return pair.Name + "=" + pair.Value;
        }

        public override String ToString()
        {
            var vals = this.Select(GetQueryStringValue).ToArray();

            return string.Join("&", vals);
        }
    }
}
