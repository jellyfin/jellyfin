using System;

public class ParsePhotoApp
{
	public static void Main (string [] args)
	{
		if(args.Length == 0) {
			Console.Error.WriteLine("USAGE: mono ParsePhoto.exe PATH [...]");
			return;
		}

		foreach (string path in args)
			ParsePhoto (path);
	}

	static void ParsePhoto (string path)
	{
		TagLib.File file = null;

		try {
			file = TagLib.File.Create(path);
		} catch (TagLib.UnsupportedFormatException) {
			Console.WriteLine ("UNSUPPORTED FILE: " + path);
			Console.WriteLine (String.Empty);
			Console.WriteLine ("---------------------------------------");
			Console.WriteLine (String.Empty);
			return;
		}

		var image = file as TagLib.Image.File;
		if (file == null) {
			Console.WriteLine ("NOT AN IMAGE FILE: " + path);
			Console.WriteLine (String.Empty);
			Console.WriteLine ("---------------------------------------");
			Console.WriteLine (String.Empty);
			return;
		}

		Console.WriteLine (String.Empty);
		Console.WriteLine (path);
		Console.WriteLine (String.Empty);

		Console.WriteLine("Tags in object  : " +  image.TagTypes);
		Console.WriteLine (String.Empty);

		Console.WriteLine("Comment         : " +  image.ImageTag.Comment);
		Console.Write("Keywords        : ");
		foreach (var keyword in image.ImageTag.Keywords) {
			Console.Write (keyword + " ");
		}
		Console.WriteLine ();
		Console.WriteLine("Rating          : " +  image.ImageTag.Rating);
		Console.WriteLine("DateTime        : " +  image.ImageTag.DateTime);
		Console.WriteLine("Orientation     : " +  image.ImageTag.Orientation);
		Console.WriteLine("Software        : " +  image.ImageTag.Software);
		Console.WriteLine("ExposureTime    : " +  image.ImageTag.ExposureTime);
		Console.WriteLine("FNumber         : " +  image.ImageTag.FNumber);
		Console.WriteLine("ISOSpeedRatings : " +  image.ImageTag.ISOSpeedRatings);
		Console.WriteLine("FocalLength     : " +  image.ImageTag.FocalLength);
		Console.WriteLine("FocalLength35mm : " +  image.ImageTag.FocalLengthIn35mmFilm);
		Console.WriteLine("Make            : " +  image.ImageTag.Make);
		Console.WriteLine("Model           : " +  image.ImageTag.Model);

		if (image.Properties != null) {
			Console.WriteLine("Width           : " +  image.Properties.PhotoWidth);
			Console.WriteLine("Height          : " +  image.Properties.PhotoHeight);
			Console.WriteLine("Type            : " +  image.Properties.Description);
		}

		Console.WriteLine ();
		Console.WriteLine("Writable?       : " +  image.Writeable.ToString ());
		Console.WriteLine("Corrupt?        : " +  image.PossiblyCorrupt.ToString ());

		if (image.PossiblyCorrupt) {
			foreach (string reason in image.CorruptionReasons) {
				Console.WriteLine ("    * " + reason);
			}
		}

		Console.WriteLine ("---------------------------------------");
	}
}
