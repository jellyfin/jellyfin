using System.IO;
using System.Text;

namespace SharpCifs.Util.Sharpen
{
    internal class OutputStreamWriter : StreamWriter
	{
		public OutputStreamWriter (OutputStream stream) : base(stream.GetWrappedStream ())
		{
		}

		public OutputStreamWriter (OutputStream stream, string encoding) : base(stream.GetWrappedStream (), Extensions.GetEncoding (encoding))
		{
		}

		public OutputStreamWriter (OutputStream stream, Encoding encoding) : base(stream.GetWrappedStream (), encoding)
		{
		}
	}
}
