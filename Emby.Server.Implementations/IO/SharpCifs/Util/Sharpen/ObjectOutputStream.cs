using System.IO;

namespace SharpCifs.Util.Sharpen
{
    internal class ObjectOutputStream : OutputStream
	{
		private BinaryWriter _bw;

		public ObjectOutputStream (OutputStream os)
		{
			_bw = new BinaryWriter (os.GetWrappedStream ());
		}

		public virtual void WriteInt (int i)
		{
			_bw.Write (i);
		}
	}
}
