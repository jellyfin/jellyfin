namespace SharpCifs.Util.Sharpen
{
    internal interface IFuture<T>
	{
		bool Cancel (bool mayInterruptIfRunning);
		T Get ();
	}
}
