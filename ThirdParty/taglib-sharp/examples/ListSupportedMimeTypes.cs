using System;
using TagLib;

public class ListSupportedMimeTypes
{
	public static void Main()
	{
		foreach(string type in SupportedMimeType.AllMimeTypes) {
			Console.WriteLine(type);
		}
	}
}

