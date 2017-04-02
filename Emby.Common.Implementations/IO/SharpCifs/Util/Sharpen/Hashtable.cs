using System.Collections.Generic;
using System.Linq;

namespace SharpCifs.Util.Sharpen
{
    public class Hashtable : Dictionary<object, object>
    {
        public void Put(object key, object value)
        {            
            Add(key, value);
        }

        public object Get(object key)
        {
            var m_key = Keys.SingleOrDefault(k => k.Equals(key));

            return m_key != null ? this[m_key] : null;
        }
    }
}
