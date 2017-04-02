using System.IO;

namespace SharpCifs.Util.Sharpen
{
    internal class FileOutputStream : OutputStream
	{
		public FileOutputStream (FilePath file): this (file.GetPath (), false)
		{
		}

		public FileOutputStream (string file): this (file, false)
		{
		}

		public FileOutputStream (FilePath file, bool append) : this(file.GetPath (), append)
		{
		}

		public FileOutputStream (string file, bool append)
		{
			try {
				if (append) {
					Wrapped = File.Open (file, FileMode.Append, FileAccess.Write);
				} else {
					Wrapped = File.Open (file, FileMode.Create, FileAccess.Write);
				}
			} catch (DirectoryNotFoundException) {
				throw new FileNotFoundException ("File not found: " + file);
			}
		}

	}
}
