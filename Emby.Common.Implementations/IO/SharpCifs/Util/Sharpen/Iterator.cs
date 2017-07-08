using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpCifs.Util.Sharpen
{
    public interface ITerator
	{
		bool HasNext ();
		object Next ();
		void Remove ();
	}

	public abstract class Iterator<T> : IEnumerator<T>, ITerator
	{
		private T _lastValue;

	    object ITerator.Next ()
		{
			return Next ();
		}

		public abstract bool HasNext ();
		public abstract T Next ();
		public abstract void Remove ();

		bool IEnumerator.MoveNext ()
		{
			if (HasNext ()) {
				_lastValue = Next ();
				return true;
			}
			return false;
		}

		void IEnumerator.Reset ()
		{
			throw new NotImplementedException ();
		}

		void IDisposable.Dispose ()
		{
		}

		T IEnumerator<T>.Current {
			get { return _lastValue; }
		}

		object IEnumerator.Current {
			get { return _lastValue; }
		}
	}
}
