using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Emby.Dlna.Server
{
    public class Headers : IDictionary<string, string>
    {
        private readonly bool _asIs = false;
        private readonly Dictionary<string, string> _dict = new Dictionary<string, string>();
        private readonly static Regex Validator = new Regex(@"^[a-z\d][a-z\d_.-]+$", RegexOptions.IgnoreCase);

        public Headers(bool asIs)
        {
            _asIs = asIs;
        }

        public Headers()
            : this(asIs: false)
        {
        }

        public int Count
        {
            get
            {
                return _dict.Count;
            }
        }
        public string HeaderBlock
        {
            get
            {
                var hb = new StringBuilder();
                foreach (var h in this)
                {
                    hb.AppendFormat("{0}: {1}\r\n", h.Key, h.Value);
                }
                return hb.ToString();
            }
        }
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
        public ICollection<string> Keys
        {
            get
            {
                return _dict.Keys;
            }
        }
        public ICollection<string> Values
        {
            get
            {
                return _dict.Values;
            }
        }


        public string this[string key]
        {
            get
            {
                return _dict[Normalize(key)];
            }
            set
            {
                _dict[Normalize(key)] = value;
            }
        }


        private string Normalize(string header)
        {
            if (!_asIs)
            {
                header = header.ToLower();
            }
            header = header.Trim();
            if (!Validator.IsMatch(header))
            {
                throw new ArgumentException("Invalid header: " + header);
            }
            return header;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public void Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }

        public void Add(string key, string value)
        {
            _dict.Add(Normalize(key), value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            var p = new KeyValuePair<string, string>(Normalize(item.Key), item.Value);
            return _dict.Contains(p);
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(Normalize(key));
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _dict.Remove(Normalize(key));
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return Remove(item.Key);
        }

        public override string ToString()
        {
            return string.Format("({0})", string.Join(", ", (from x in _dict
                                                             select string.Format("{0}={1}", x.Key, x.Value))));
        }

        public bool TryGetValue(string key, out string value)
        {
            return _dict.TryGetValue(Normalize(key), out value);
        }

        public string GetValueOrDefault(string key, string defaultValue)
        {
            string val;

            if (TryGetValue(key, out val))
            {
                return val;
            }

            return defaultValue;
        }
    }
}
