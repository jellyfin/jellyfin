using System;
using TagLib;

public class SetPictures
{
	public static void Main(string [] args)
	{
		if(args.Length < 2) {
			Console.Error.WriteLine("USAGE: mono SetPictures.exe AUDIO_PATH IMAGE_PATH_1[...IMAGE_PATH_N]");
			return;
		}
	
		TagLib.File file = TagLib.File.Create(args[0]);
		Console.WriteLine("Current picture count: " + file.Tag.Pictures.Length);
		
		Picture [] pictures = new Picture[args.Length - 1];
	
		for(int i = 1; i < args.Length; i++) {
			Picture picture = new Picture(args[i]);
			pictures[i - 1] = picture;
		}
		
		file.Tag.Pictures = pictures;
		file.Save();
		
		Console.WriteLine("New picture count: " + file.Tag.Pictures.Length);
	}
}
