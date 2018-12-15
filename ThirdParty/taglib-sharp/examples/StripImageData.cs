
using System;
using TagLib;

public class StripImageData
{
	private static byte[] image_data = new byte[] {
		0xFF, 0xDA, 0x00, 0x0C, 0x03, 0x01, 0x00,
		0x02, 0x11, 0x03, 0x11, 0x00, 0x3F, 0x00,
		0x8C, 0x80, 0x07, 0xFF, 0xD9
	};

	public static void Main (string [] args)
	{
		if (args.Length != 1) {
			Console.Out.WriteLine ("usage: mono StripImageData.exe [jpegfile]");
			return;
		}

		ImageFile file = new ImageFile (args [0]);

		file.Mode = File.AccessMode.Write;

		long greatest_segment_position = 0;
		long greatest_segment_length = 0;

		// collect data segments
		while (true) {

			long sos = file.Find (new byte [] {0xFF, 0xDA}, file.Tell);

			if (sos == -1)
				break;

			file.Seek (sos);

			long segment_length = SkipDataSegment (file);

			if (segment_length > greatest_segment_length) {
				greatest_segment_length = segment_length;
				greatest_segment_position = sos;
			}
		}

		if (greatest_segment_length == 0)
		{
			Console.Out.WriteLine ("doesn't look like an jpeg file");
			return;
		}

		System.Console.WriteLine ("Stripping data segment at {0}", greatest_segment_position);

		file.RemoveBlock (greatest_segment_position, greatest_segment_length);
		file.Seek (greatest_segment_position);
		file.WriteBlock (image_data);
		file.Mode = File.AccessMode.Closed;
	}

	private static long SkipDataSegment (ImageFile file)
	{
		long position = file.Tell;

		// skip sos maker
		if (file.ReadBlock (2).ToUInt () != 0xFFDA)
			throw new Exception (String.Format ("Not a data segment at position: {0}", position));

		while (true) {
			if (0xFF == (byte) file.ReadBlock (1)[0]) {
				byte maker = (byte) file.ReadBlock (1)[0];

				if (maker != 0x00 && (maker <= 0xD0 || maker >= 0xD7))
					break;
			}
		}

		long length = file.Tell - position - 2;

		System.Console.WriteLine ("Data segment of length {0} found at {1}", length, position);

		return length;
	}

	private class ImageFile : File {

		// Hacky implementation to make use of some methods defined in TagLib.File

		public ImageFile (string path)
		: base (new File.LocalFileAbstraction (path)) {}

		public override Tag GetTag (TagLib.TagTypes type, bool create)
		{
			throw new System.NotImplementedException ();
		}

		public override Properties Properties {
			get {
				throw new System.NotImplementedException ();
			}
		}

		public override void RemoveTags (TagLib.TagTypes types)
		{
			throw new System.NotImplementedException ();
		}

		public override void Save ()
		{
			throw new System.NotImplementedException ();
		}

		public override Tag Tag {
			get {
				throw new System.NotImplementedException ();
			}
		}
	}
}