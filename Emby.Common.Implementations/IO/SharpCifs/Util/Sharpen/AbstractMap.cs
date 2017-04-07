using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SharpCifs.Util.Sharpen
{
    public abstract class AbstractMap<T, TU> : IDictionary<T, TU>
	{
	    public virtual void Clear ()
		{
			EntrySet ().Clear ();
		}

		public virtual bool ContainsKey (object name)
		{
			return EntrySet ().Any (p => p.Key.Equals ((T)name));
		}

		public abstract ICollection<KeyValuePair<T, TU>> EntrySet ();

		public virtual TU Get (object key)
		{
			return EntrySet ().Where (p => p.Key.Equals (key)).Select (p => p.Value).FirstOrDefault ();
		}

		protected virtual IEnumerator<KeyValuePair<T, TU>> InternalGetEnumerator ()
		{
			return EntrySet ().GetEnumerator ();
		}

		public virtual bool IsEmpty ()
		{
			return !EntrySet ().Any ();
		}

		public virtual TU Put (T key, TU value)
		{
			throw new NotSupportedException ();
		}

		public virtual TU Remove (object key)
		{
			Iterator<TU> iterator = EntrySet () as Iterator<TU>;
			if (iterator == null) {
				throw new NotSupportedException ();
			}
			while (iterator.HasNext ()) {
				TU local = iterator.Next ();
				if (local.Equals ((T)key)) {
					iterator.Remove ();
					return local;
				}
			}
			return default(TU);
		}

		void ICollection<KeyValuePair<T, TU>>.Add (KeyValuePair<T, TU> item)
		{
			Put (item.Key, item.Value);
		}

		bool ICollection<KeyValuePair<T, TU>>.Contains (KeyValuePair<T, TU> item)
		{
			throw new NotImplementedException ();
		}

		void ICollection<KeyValuePair<T, TU>>.CopyTo (KeyValuePair<T, TU>[] array, int arrayIndex)
		{
			EntrySet ().CopyTo (array, arrayIndex);
		}

		bool ICollection<KeyValuePair<T, TU>>.Remove (KeyValuePair<T, TU> item)
		{
			Remove (item.Key);
			return true;
		}

		void IDictionary<T, TU>.Add (T key, TU value)
		{
			Put (key, value);
		}

		bool IDictionary<T, TU>.ContainsKey (T key)
		{
			return ContainsKey (key);
		}

		bool IDictionary<T, TU>.Remove (T key)
		{
			if (ContainsKey (key)) {
				Remove (key);
				return true;
			}
			return false;
		}

		bool IDictionary<T, TU>.TryGetValue (T key, out TU value)
		{
			if (ContainsKey (key)) {
				value = Get (key);
				return true;
			}
			value = default(TU);
			return false;
		}

		IEnumerator<KeyValuePair<T, TU>> IEnumerable<KeyValuePair<T, TU>>.GetEnumerator ()
		{
			return InternalGetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return InternalGetEnumerator ();
		}

		public virtual int Count {
			get { return EntrySet ().Count; }
		}

		public TU this[T key] {
			get { return Get (key); }
			set { Put (key, value); }
		}

		public virtual IEnumerable<T> Keys {
			get { return EntrySet ().Select (p => p.Key); }
		}

		int ICollection<KeyValuePair<T, TU>>.Count {
			get { return Count; }
		}

		bool ICollection<KeyValuePair<T, TU>>.IsReadOnly {
			get { return false; }
		}

		ICollection<T> IDictionary<T, TU>.Keys {
			get { return Keys.ToList (); }
		}

		ICollection<TU> IDictionary<T, TU>.Values {
			get { return Values.ToList (); }
		}

		public virtual IEnumerable<TU> Values {
			get { return EntrySet ().Select (p => p.Value); }
		}
	}
}
