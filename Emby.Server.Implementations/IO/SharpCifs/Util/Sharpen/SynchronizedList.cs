using System.Collections;
using System.Collections.Generic;

namespace SharpCifs.Util.Sharpen
{
    internal class SynchronizedList<T> : IList<T>
	{
		private IList<T> _list;

		public SynchronizedList (IList<T> list)
		{
			this._list = list;
		}

		public int IndexOf (T item)
		{
			lock (_list) {
				return _list.IndexOf (item);
			}
		}

		public void Insert (int index, T item)
		{
			lock (_list) {
				_list.Insert (index, item);
			}
		}

		public void RemoveAt (int index)
		{
			lock (_list) {
				_list.RemoveAt (index);
			}
		}

		void ICollection<T>.Add (T item)
		{
			lock (_list) {
				_list.Add (item);
			}
		}

		void ICollection<T>.Clear ()
		{
			lock (_list) {
				_list.Clear ();
			}
		}

		bool ICollection<T>.Contains (T item)
		{
			lock (_list) {
				return _list.Contains (item);
			}
		}

		void ICollection<T>.CopyTo (T[] array, int arrayIndex)
		{
			lock (_list) {
				_list.CopyTo (array, arrayIndex);
			}
		}

		bool ICollection<T>.Remove (T item)
		{
			lock (_list) {
				return _list.Remove (item);
			}
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator ()
		{
			return _list.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return _list.GetEnumerator ();
		}

		public T this[int index] {
			get {
				lock (_list) {
					return _list[index];
				}
			}
			set {
				lock (_list) {
					_list[index] = value;
				}
			}
		}

		int ICollection<T>.Count {
			get {
				lock (_list) {
					return _list.Count;
				}
			}
		}

		bool ICollection<T>.IsReadOnly {
			get { return _list.IsReadOnly; }
		}
	}
}
