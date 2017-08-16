using System.Threading;

namespace SharpCifs.Util.Sharpen
{
    internal class ReentrantLock
	{
		public void Lock ()
		{
			Monitor.Enter (this);
		}

		public bool TryLock ()
		{
			return Monitor.TryEnter (this);
		}

		public void Unlock ()
		{
			Monitor.Exit (this);
		}
	}
}
