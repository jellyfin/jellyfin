namespace SharpCifs.Util.Sharpen
{
    internal class PipedOutputStream : OutputStream
	{
		PipedInputStream _ips;

		public PipedOutputStream ()
		{
		}

		public PipedOutputStream (PipedInputStream iss) : this()
		{
			Attach (iss);
		}

		public override void Close ()
		{
			_ips.Close ();
			base.Close ();
		}

		internal void Attach (PipedInputStream iss)
		{
			_ips = iss;
		}

		public override void Write (int b)
		{
			_ips.Write (b);
		}

		public override void Write (byte[] b, int offset, int len)
		{
			_ips.Write (b, offset, len);
		}
	}
}
