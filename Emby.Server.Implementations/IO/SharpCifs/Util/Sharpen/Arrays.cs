using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpCifs.Util.Sharpen
{
    public class Arrays
	{
		public static List<T> AsList<T> (params T[] array)
		{
			return array.ToList ();
		}

		public static bool Equals<T> (T[] a1, T[] a2)
		{
			if (a1.Length != a2.Length) {
				return false;
			}
		    return !a1.Where((t, i) => !t.Equals(a2[i])).Any();
		}

		public static void Fill<T> (T[] array, T val)
		{
			Fill (array, 0, array.Length, val);
		}

		public static void Fill<T> (T[] array, int start, int end, T val)
		{
			for (int i = start; i < end; i++) {
				array[i] = val;
			}
		}

		public static void Sort (string[] array)
		{
			Array.Sort (array, (s1,s2) => string.CompareOrdinal (s1,s2));
		}

		public static void Sort<T> (T[] array)
		{
			Array.Sort (array);
		}

		public static void Sort<T> (T[] array, IComparer<T> c)
		{
			Array.Sort (array, c);
		}

		public static void Sort<T> (T[] array, int start, int count)
		{
			Array.Sort (array, start, count);
		}

		public static void Sort<T> (T[] array, int start, int count, IComparer<T> c)
		{
			Array.Sort (array, start, count, c);
		}
	}
}
