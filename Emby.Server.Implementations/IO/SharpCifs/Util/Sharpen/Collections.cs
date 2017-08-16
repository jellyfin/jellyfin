using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SharpCifs.Util.Sharpen
{
    internal static class Collections<T>
	{
		static readonly IList<T> Empty = new T [0];
		public static IList<T> EmptySet {
			get { return Empty; }
		}
		
	}
	
	public static class Collections
	{
		public static bool AddAll<T> (ICollection<T> list, IEnumerable toAdd)
		{
			foreach (T t in toAdd)
				list.Add (t);
			return true;
		}

		public static TV Remove<TK, TV> (IDictionary<TK, TV> map, TK toRemove) where TK : class
		{
			TV local;
			if (map.TryGetValue (toRemove, out local)) {
				map.Remove (toRemove);
				return local;
			}
			return default(TV);
		}


		public static T[] ToArray<T> (ICollection<T> list)
		{
			T[] array = new T[list.Count];
			list.CopyTo (array, 0);
			return array;
		}

        public static T[] ToArray<T>(List<object> list)
        {
            T[] array = new T[list.Count];
            for(int c = 0; c < list.Count; c++)
            {
                array[c] = (T)list[c];
            }

            return array;
        }


		public static TU[] ToArray<T,TU> (ICollection<T> list, TU[] res) where T:TU
		{
			if (res.Length < list.Count)
				res = new TU [list.Count];
			
			int n = 0;
			foreach (T t in list)
				res [n++] = t;
			
			if (res.Length > list.Count)
				res [list.Count] = default (T);
			return res;
		}
		
		public static IDictionary<TK,TV> EmptyMap<TK,TV> ()
		{
			return new Dictionary<TK,TV> ();
		}

		public static IList<T> EmptyList<T> ()
		{
			return Collections<T>.EmptySet;
		}

		public static ICollection<T> EmptySet<T> ()
		{
			return Collections<T>.EmptySet;
		}

		public static IList<T> NCopies<T> (int n, T elem)
		{
			List<T> list = new List<T> (n);
			while (n-- > 0) {
				list.Add (elem);
			}
			return list;
		}

		public static void Reverse<T> (IList<T> list)
		{
			int end = list.Count - 1;
			int index = 0;
			while (index < end) {
				T tmp = list [index];
				list [index] = list [end];
				list [end] = tmp;
				++index;
				--end;
			}
		}

		public static ICollection<T> Singleton<T> (T item)
		{
			List<T> list = new List<T> (1);
			list.Add (item);
			return list;
		}

		public static IList<T> SingletonList<T> (T item)
		{
			List<T> list = new List<T> (1);
			list.Add (item);
			return list;
		}

		public static IList<T> SynchronizedList<T> (IList<T> list)
		{
			return new SynchronizedList<T> (list);
		}

		public static ICollection<T> UnmodifiableCollection<T> (ICollection<T> list)
		{
			return list;
		}

		public static IList<T> UnmodifiableList<T> (IList<T> list)
		{
			return new ReadOnlyCollection<T> (list);
		}

		public static ICollection<T> UnmodifiableSet<T> (ICollection<T> list)
		{
			return list;
		}
		
		public static IDictionary<TK,TV> UnmodifiableMap<TK,TV> (IDictionary<TK,TV> dict)
		{
			return dict;
		}

    }
}
