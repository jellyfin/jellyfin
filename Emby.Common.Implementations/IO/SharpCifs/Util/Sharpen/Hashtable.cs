using System.Collections.Generic;
using System.Linq;

namespace SharpCifs.Util.Sharpen
{
    public class Hashtable : Dictionary<object, object>
    {
        public void Put(object key, object value)
        {
            if (this.ContainsKey(key))
                this[key] = value;
            else
                this.Add(key, value);
        }

        public object Get(object key)
        {
            return this.ContainsKey(key) 
                ? this[key] 
                : null;
        }
    }
}
