using System;
using NUnit.Framework;
using TagLib;

namespace TagLib.Tests.FileFormats
{   
	public static class ExtendedTests
	{      
		public static void WriteExtendedTags (string sample_file, string tmp_file)
		{
			if (System.IO.File.Exists (tmp_file))
				System.IO.File.Delete (tmp_file);
			
			try {
				System.IO.File.Copy(sample_file, tmp_file);
				
				File tmp = File.Create (tmp_file);
				SetTags (tmp.Tag);
				tmp.Save ();
				
				tmp = File.Create (tmp_file);
				CheckTags (tmp.Tag);
			} finally {
				if (System.IO.File.Exists (tmp_file))
					System.IO.File.Delete (tmp_file);
			}
		}
		
		public static void SetTags (Tag tag)
		{
						tag.ReplayGainTrackGain = -10.28;
						tag.ReplayGainTrackPeak = 0.999969;
						tag.ReplayGainAlbumGain = -9.98;
						tag.ReplayGainAlbumPeak = 0.999980;
				
		}
		
		public static void CheckTags (Tag tag)
		{
			Assert.AreEqual (-10.28, tag.ReplayGainTrackGain);
						Assert.AreEqual(0.999969, tag.ReplayGainTrackPeak);
						Assert.AreEqual(-9.98, tag.ReplayGainAlbumGain);
						Assert.AreEqual(0.999980, tag.ReplayGainAlbumPeak);
				}
		
		public static void TestCorruptionResistance (string path)
		{
			try {
				File.Create (path);
			} catch(CorruptFileException) {
			} catch(NullReferenceException e) {
				throw e;
			} catch {
			}
		}
	}
}
