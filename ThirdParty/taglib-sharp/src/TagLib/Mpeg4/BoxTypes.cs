//
// BoxTypes.cs: Contains common box names.
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2006-2007 Brian Nickel
// 
// This library is free software; you can redistribute it and/or modify
// it  under the terms of the GNU Lesser General Public License version
// 2.1 as published by the Free Software Foundation.
//
// This library is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307
// USA
//

namespace TagLib.Mpeg4 {
	/// <summary>
	///    <see cref="BoxType" /> provides references to different box types
	///    used by the library.
	/// </summary>
	/// <remarks>
	///    <para>This class is used to severely reduce the number of times
	///    these types are created in <see cref="AppleTag" />, greatly
	///    improving the speed at which warm files are read.</para>
	///    <para>The reason it is marked as internal is because I'm not sure
	///    I like the way the fields are named, and it is really
	///    unneccessary for external uses. While the library may use
	///    <c>DataBoxes (BoxType.Gen, BoxType.Gnre);</c>, an external user
	///    could use <c>tag.DataBoxes ("gen", "gnre");</c> with the same
	///    result.</para>
	///    <see url="https://picard.musicbrainz.org/docs/mappings/"/> 
	/// </remarks>
	internal static class BoxType
	{
		public static readonly ReadOnlyByteVector Aart = "aART";
		public static readonly ReadOnlyByteVector Alb  = AppleTag.FixId ("alb");
		public static readonly ReadOnlyByteVector Art  = AppleTag.FixId ("ART");
		public static readonly ReadOnlyByteVector Cmt  = AppleTag.FixId ("cmt");
		public static readonly ReadOnlyByteVector Cond = "cond";
		public static readonly ReadOnlyByteVector Covr = "covr";
		public static readonly ReadOnlyByteVector Co64 = "co64";
		public static readonly ReadOnlyByteVector Cpil = "cpil";
		public static readonly ReadOnlyByteVector Cprt = "cprt";
		public static readonly ReadOnlyByteVector Data = "data";
		public static readonly ReadOnlyByteVector Day  = AppleTag.FixId ("day");
		public static readonly ReadOnlyByteVector Desc = "desc";
		public static readonly ReadOnlyByteVector Disk = "disk";
		public static readonly ReadOnlyByteVector Dtag = "dtag";
		public static readonly ReadOnlyByteVector Esds = "esds";
		public static readonly ReadOnlyByteVector Ilst = "ilst";
		public static readonly ReadOnlyByteVector Free = "free";
		public static readonly ReadOnlyByteVector Gen  = AppleTag.FixId ("gen");
		public static readonly ReadOnlyByteVector Gnre = "gnre";
		public static readonly ReadOnlyByteVector Grp  = AppleTag.FixId("grp");
		public static readonly ReadOnlyByteVector Hdlr = "hdlr";
		public static readonly ReadOnlyByteVector Lyr  = AppleTag.FixId ("lyr");
		public static readonly ReadOnlyByteVector Mdat = "mdat";
		public static readonly ReadOnlyByteVector Mdia = "mdia";
		public static readonly ReadOnlyByteVector Meta = "meta";
		public static readonly ReadOnlyByteVector Mean = "mean";
		public static readonly ReadOnlyByteVector Minf = "minf";
		public static readonly ReadOnlyByteVector Moov = "moov";
		public static readonly ReadOnlyByteVector Mvhd = "mvhd";
		public static readonly ReadOnlyByteVector Nam  = AppleTag.FixId ("nam");
		public static readonly ReadOnlyByteVector Name = "name";
		public static readonly ReadOnlyByteVector Role = "role";
		public static readonly ReadOnlyByteVector Skip = "skip";
		public static readonly ReadOnlyByteVector Soaa = "soaa"; // Album Artist Sort
		public static readonly ReadOnlyByteVector Soar = "soar"; // Performer Sort
		public static readonly ReadOnlyByteVector Soco = "soco"; // Composer Sort
		public static readonly ReadOnlyByteVector Sonm = "sonm"; // Track Title Sort
		public static readonly ReadOnlyByteVector Soal = "soal"; // Album Title Sort
		public static readonly ReadOnlyByteVector Stbl = "stbl";
		public static readonly ReadOnlyByteVector Stco = "stco";
		public static readonly ReadOnlyByteVector Stsd = "stsd";
		public static readonly ReadOnlyByteVector Subt = "Subt";
		public static readonly ReadOnlyByteVector Text = "text";
		public static readonly ReadOnlyByteVector Tmpo = "tmpo";
		public static readonly ReadOnlyByteVector Trak = "trak";
		public static readonly ReadOnlyByteVector Trkn = "trkn";
		public static readonly ReadOnlyByteVector Udta = "udta";
		public static readonly ReadOnlyByteVector Url = AppleTag.FixId ("url");
		public static readonly ReadOnlyByteVector Uuid = "uuid";
		public static readonly ReadOnlyByteVector Wrt  = AppleTag.FixId ("wrt");
		public static readonly ReadOnlyByteVector DASH = "----";

		// Handler types.
		public static readonly ReadOnlyByteVector Soun = "soun";
		public static readonly ReadOnlyByteVector Vide = "vide";

		// Another handler type, found in wild in audio file ripped using iTunes
		public static readonly ReadOnlyByteVector Alis = "alis";
	}
}
