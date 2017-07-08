using System;
using System.IO;

namespace SharpCifs.Util.Sharpen
{
    internal class ObjectInputStream : InputStream
	{
		private BinaryReader _reader;

		public ObjectInputStream (InputStream s)
		{
			_reader = new BinaryReader (s.GetWrappedStream ());
		}

		public int ReadInt ()
		{
			return _reader.ReadInt32 ();
		}

		public object ReadObject ()
		{
			throw new NotImplementedException ();
		}
	}
}
