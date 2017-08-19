using System.Collections.Generic;

namespace SharpCifs.Util.Sharpen
{
    internal interface IConcurrentMap<T, TU> : IDictionary<T, TU>
	{
		TU PutIfAbsent (T key, TU value);
		bool Remove (object key, object value);
		bool Replace (T key, TU oldValue, TU newValue);
	}
}
