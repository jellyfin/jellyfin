namespace SharpCifs.Util.Sharpen
{
    public class FilterOutputStream : OutputStream
	{
		protected OutputStream Out;

		public FilterOutputStream (OutputStream os)
		{
			Out = os;
		}

		public override void Close ()
		{
			Out.Close ();
		}

		public override void Flush ()
		{
			Out.Flush ();
		}

		public override void Write (byte[] b)
		{
			Out.Write (b);
		}

		public override void Write (int b)
		{
			Out.Write (b);
		}

		public override void Write (byte[] b, int offset, int len)
		{
			Out.Write (b, offset, len);
		}
	}
}
