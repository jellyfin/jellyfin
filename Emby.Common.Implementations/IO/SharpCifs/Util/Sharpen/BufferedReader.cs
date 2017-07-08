using System.IO;

namespace SharpCifs.Util.Sharpen
{
    public class BufferedReader : StreamReader
	{
		public BufferedReader (InputStreamReader r) : base(r.BaseStream)
		{
		}
	}
}
