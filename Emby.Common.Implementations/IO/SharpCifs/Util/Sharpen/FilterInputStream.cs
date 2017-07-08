namespace SharpCifs.Util.Sharpen
{
    public class FilterInputStream : InputStream
	{
		protected InputStream In;

		public FilterInputStream (InputStream s)
		{
			In = s;
		}

		public override int Available ()
		{
			return In.Available ();
		}

		public override void Close ()
		{
			In.Close ();
		}

		public override void Mark (int readlimit)
		{
			In.Mark (readlimit);
		}

		public override bool MarkSupported ()
		{
			return In.MarkSupported ();
		}

		public override int Read ()
		{
			return In.Read ();
		}

		public override int Read (byte[] buf)
		{
			return In.Read (buf);
		}

		public override int Read (byte[] b, int off, int len)
		{
			return In.Read (b, off, len);
		}

		public override void Reset ()
		{
			In.Reset ();
		}

		public override long Skip (long cnt)
		{
			return In.Skip (cnt);
		}
	}
}
