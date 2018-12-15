using System;
using System.Collections.Generic;
using TagLib;

public class BatchSet
{
	private enum Mode {
		Tag, Value, File
	}
	
	public static void Main(string [] args)
	{
		if(args.Length < 3) {
			Console.Error.WriteLine ("USAGE: BatchSet.exe -tag value [-tag2 value ...] File1 [File2 ...]");
			return;
		}
		
		Mode mode = Mode.Tag;
		List<string> files = new List<string> ();
		Dictionary<string,string> tags  = new Dictionary<string,string> ();
		
		string tag = null;
		
		foreach (string str in args) {
			if (mode == Mode.Tag) {
				if (str [0] == '-') {
					if (str == "--") {
						mode = Mode.File;
					} else {
						tag = str.Substring (1);
						mode = Mode.Value;
					}
					
					continue;
				}
				mode = Mode.File;
			}
			
			if (mode == Mode.Value) {
				if (!tags.ContainsKey (tag))
					tags.Add (tag, str);
				mode = Mode.Tag;
				continue;
			}
			
			if (mode == Mode.File)
				files.Add (str);
		}
		
		foreach (string filename in files) {
			TagLib.File file = TagLib.File.Create (filename);
			if (file == null)
				continue;
			
			Console.WriteLine ("Updating Tags For: " + filename);
		
			foreach (string key in tags.Keys) {
				string value = tags [key];
				try {
					switch (key) {
					case "id3version":
						byte number = byte.Parse (value);
						if (number == 1) {
							file.RemoveTags (TagTypes.Id3v2);
						} else {
							TagLib.Id3v2.Tag v2 =
								file.GetTag (TagTypes.Id3v2, true)
								as TagLib.Id3v2.Tag;
							
							if (v2 != null)
								v2.Version = number;
						}
						break;
					case "album":
						file.Tag.Album = value;
						break;
					case "artists":
						file.Tag.AlbumArtists = value.Split (new char [] {';'});
						break;
					case "comment":
						file.Tag.Comment = value;
						break;
					case "lyrics":
						file.Tag.Lyrics = value;
						break;
					case "composers":
						file.Tag.Composers = value.Split (new char [] {';'});
						break;
					case "disc":
						file.Tag.Disc = uint.Parse (value);
						break;
					case "disccount":
						file.Tag.DiscCount = uint.Parse (value);
						break;
					case "genres":
						file.Tag.Genres = value.Split (new char [] {';'});
						break;
					case "performers":
						file.Tag.Performers = value.Split (new char [] {';'});
						break;
					case "title":
						file.Tag.Title = value;
						break;
					case "track":
						file.Tag.Track = uint.Parse (value);
						break;
					case "trackcount":
						file.Tag.TrackCount = uint.Parse (value);
						break;
					case "year":
						file.Tag.Year = uint.Parse (value);
						break;
					case "pictures":
						List<Picture> pics = new List<Picture> ();
						if (!string.IsNullOrEmpty (value))
							foreach (string path in value.Split (new char [] {';'})) {
								pics.Add (new Picture (path));
							}
						file.Tag.Pictures = pics.ToArray ();
						break;
					}
				} catch (Exception e) {
					Console.WriteLine ("Error setting tag " + key + ":");
					Console.WriteLine (e);
				}
			}
			
			file.Save();
		}
	}
}
