using System.Collections.Generic;

namespace SharpCifs.Util.Sharpen
{
    internal class ConcurrentHashMap<T, TU> : AbstractMap<T, TU>, IConcurrentMap<T, TU>
	{
		private Dictionary<T, TU> _table;

		public ConcurrentHashMap ()
		{
			_table = new Dictionary<T, TU> ();
		}

		public ConcurrentHashMap (int initialCapacity, float loadFactor, int concurrencyLevel)
		{
			_table = new Dictionary<T, TU> (initialCapacity);
		}

		public override void Clear ()
		{
			lock (_table) {
				_table = new Dictionary<T, TU> ();
			}
		}

		public override bool ContainsKey (object name)
		{
			return _table.ContainsKey ((T)name);
		}

		public override ICollection<KeyValuePair<T, TU>> EntrySet ()
		{
			return this;
		}

		public override TU Get (object key)
		{
			TU local;
			_table.TryGetValue ((T)key, out local);
			return local;
		}

		protected override IEnumerator<KeyValuePair<T, TU>> InternalGetEnumerator ()
		{
			return _table.GetEnumerator ();
		}

		public override bool IsEmpty ()
		{
			return _table.Count == 0;
		}

		public override TU Put (T key, TU value)
		{
			lock (_table) {
				TU old = Get (key);
				Dictionary<T, TU> newTable = new Dictionary<T, TU> (_table);
				newTable[key] = value;
				_table = newTable;
				return old;
			}
		}

		public TU PutIfAbsent (T key, TU value)
		{
			lock (_table) {
				if (!ContainsKey (key)) {
					Dictionary<T, TU> newTable = new Dictionary<T, TU> (_table);
					newTable[key] = value;
					_table = newTable;
					return value;
				}
				return Get (key);
			}
		}

		public override TU Remove (object key)
		{
			lock (_table) {
				TU old = Get ((T)key);
				Dictionary<T, TU> newTable = new Dictionary<T, TU> (_table);
				newTable.Remove ((T)key);
				_table = newTable;
				return old;
			}
		}

		public bool Remove (object key, object value)
		{
			lock (_table) {
				if (ContainsKey (key) && value.Equals (Get (key))) {
					Dictionary<T, TU> newTable = new Dictionary<T, TU> (_table);
					newTable.Remove ((T)key);
					_table = newTable;
					return true;
				}
				return false;
			}
		}

		public bool Replace (T key, TU oldValue, TU newValue)
		{
			lock (_table) {
				if (ContainsKey (key) && oldValue.Equals (Get (key))) {
					Dictionary<T, TU> newTable = new Dictionary<T, TU> (_table);
					newTable[key] = newValue;
					_table = newTable;
					return true;
				}
				return false;
			}
		}

		public override IEnumerable<T> Keys {
			get { return _table.Keys; }
		}

		public override IEnumerable<TU> Values {
			get { return _table.Values; }
		}
	}
}
