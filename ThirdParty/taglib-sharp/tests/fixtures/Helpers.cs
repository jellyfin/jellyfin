using System;
using System.IO;

namespace TagLib.Tests
{
	public class TestPath
	{
		#region OS-Independent Path composition

		public static readonly string TestsDir = Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(Debugger)).Location);
		public static readonly string Samples = Path.Combine(TestsDir, "samples", " ").TrimEnd();
		public static readonly string Covers = Path.Combine(TestsDir, "..", "examples", "covers", " ").TrimEnd();

		#endregion
	}



	public class Debugger
	{

		public static void DumpHex (ByteVector data)
		{
			DumpHex (data.Data);
		}
		
		public static void DumpHex (byte [] data)
		{
		        int cols = 16;
		        int rows = data.Length / cols +
		        	(data.Length % cols != 0 ? 1 : 0);
			
			for (int row = 0; row < rows; row ++) {
				for (int col = 0; col < cols; col ++) {
					if (row == rows - 1 &&
						data.Length % cols != 0 &&
						col >= data.Length % cols)
						Console.Write ("   ");
					else
						Console.Write (" {0:x2}",
							data [row * cols + col]);
				}
				
				Console.Write (" | ");
				
				for (int col = 0; col < cols; col ++) {
					if (row == rows - 1 &&
						data.Length % cols != 0 &&
						col >= data.Length % cols)
						Console.Write (" ");
					else
						WriteByte2 (
							data [row * cols + col]);
				}
				
				Console.WriteLine ();
			}
			Console.WriteLine ();
		}

		private static void WriteByte2 (byte data)
		{
			foreach (char c in allowed)
				if (c == data) {
					Console.Write (c);
					return;
				}
			
			Console.Write (".");
		}
		
		private static string allowed = "0123456789abcdefghijklmnopqr" +
			"stuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ`~!@#$%^&*()_+-={}" +
			"[];:'\",.<>?/\\|";
	}
	
	public class MemoryFileAbstraction : File.IFileAbstraction
	{
		private System.IO.MemoryStream stream;
		
		public MemoryFileAbstraction (int maxSize, byte [] data)
		{
			stream = new System.IO.MemoryStream (maxSize);
			stream.Write (data, 0, data.Length);
			stream.Position = 0;
		}
		
		public string Name {
			get {return "MEMORY";}
		}
		
		public System.IO.Stream ReadStream {
			get {return stream;}
		}
		
		public System.IO.Stream WriteStream {
			get {return stream;}
		}
		
		public void CloseStream (System.IO.Stream stream)
		{
		}
	}
	
	public class CodeTimer : IDisposable
	{
		private DateTime start;
		private TimeSpan elapsed_time = TimeSpan.Zero;
		private string label;
		
		public CodeTimer()
		{
			start = DateTime.Now;
		}
		
		public CodeTimer(string label) : this()
		{
			this.label = label;
		}
		
		public TimeSpan ElapsedTime {
			get { 
				DateTime now = DateTime.Now;
				return elapsed_time == TimeSpan.Zero ?
					now - start : elapsed_time;
			}
		}
		
		public void WriteElapsed(string message)
		{
			Console.WriteLine("{0} {1} {2}", label, message,
				ElapsedTime);
		}
		
		public void Dispose()
		{
			elapsed_time = DateTime.Now - start;
			if(label != null) {
				WriteElapsed("timer stopped:");
			}
		}
	}
}
