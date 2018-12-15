//
// WaveFormatEx.cs:
//
// Author:
//   Brian Nickel (brian.nickel@gmail.com)
//
// Copyright (C) 2007 Brian Nickel
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

using System;

namespace TagLib.Riff {
	/// <summary>
	///    This structure provides a representation of a Microsoft
	///    WaveFormatEx structure.
	/// </summary>
	public struct WaveFormatEx : IAudioCodec, ILosslessAudioCodec
	{
#region Private Fields
		
		/// <summary>
		///    Contains the format tag of the audio.
		/// </summary>
		ushort format_tag;
		
		/// <summary>
		///    Contains the number of audio channels.
		/// </summary>
		ushort channels;
		
		/// <summary>
		///    Contains the number of samples per second.
		/// </summary>
		uint samples_per_second;
		
		/// <summary>
		///    Contains the average number of bytes per second.
		/// </summary>
		uint   average_bytes_per_second;
		
		/// <summary>
		///    Contains the number of bits per sample.
		/// </summary>
		ushort bits_per_sample;
		
#endregion
		
		
		
#region Constructors
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="WaveFormatEx" /> by reading the raw structure from
		///    the beginning of a <see cref="ByteVector" /> object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    data structure.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> contains less than 16 bytes.
		/// </exception>
		[Obsolete("Use WaveFormatEx(ByteVector,int)")]
		public WaveFormatEx (ByteVector data) : this (data, 0)
		{
		}
		
		/// <summary>
		///    Constructs and initializes a new instance of <see
		///    cref="WaveFormatEx" /> by reading the raw structure from
		///    a specified position in a <see cref="ByteVector" />
		///    object.
		/// </summary>
		/// <param name="data">
		///    A <see cref="ByteVector" /> object containing the raw
		///    data structure.
		/// </param>
		/// <param name="offset">
		///    A <see cref="int" /> value specifying the index in
		///    <paramref name="data"/> at which the structure begins.
		/// </param>
		/// <exception cref="ArgumentNullException">
		///    <paramref name="data" /> is <see langword="null" />.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///    <paramref name="offset" /> is less than zero.
		/// </exception>
		/// <exception cref="CorruptFileException">
		///    <paramref name="data" /> contains less than 16 bytes at
		///    <paramref name="offset" />.
		/// </exception>
		public WaveFormatEx (ByteVector data, int offset)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			
			if (offset < 0)
				throw new ArgumentOutOfRangeException (
					"offset");
			
			if (offset + 16 > data.Count)
				throw new CorruptFileException (
					"Expected 16 bytes.");
			
			format_tag = data.Mid (offset, 2).ToUShort (false);
			channels = data.Mid (offset + 2, 2).ToUShort (false);
			samples_per_second = data.Mid (offset + 4, 4)
				.ToUInt (false);
			average_bytes_per_second = data.Mid (offset + 8, 4)
				.ToUInt (false);
			bits_per_sample = data.Mid (offset + 14, 2)
				.ToUShort (false);
		}
		
#endregion
		
		
		
#region Public Properties
		
		/// <summary>
		///    Gets the format tag of the audio described by the
		///    current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="ushort" /> value containing the format tag
		///    of the audio.
		/// </returns>
		/// <remarks>
		///    Format tags indicate the codec of the audio contained in
		///    the file and are contained in a Microsoft registry. For
		///    a description of the format, use <see cref="Description"
		///    />.
		/// </remarks>
		public ushort FormatTag {
			get {return format_tag;}
		}
		
		/// <summary>
		///    Gets the average bytes per second of the audio described
		///    by the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="ushort" /> value containing the average
		///    bytes per second of the audio.
		/// </returns>
		public uint AverageBytesPerSecond {
			get {return average_bytes_per_second;}
		}
		
		/// <summary>
		///    Gets the bits per sample of the audio described by the
		///    current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="ushort" /> value containing the bits per
		///    sample of the audio.
		/// </returns>
		public ushort BitsPerSample {
			get {return bits_per_sample;}
		}
		
#endregion

#region ILosslessAudioCodec
		
		int ILosslessAudioCodec.BitsPerSample {
			get {return bits_per_sample;}
		}
		
#endregion
		
#region IAudioCodec
		
		/// <summary>
		///    Gets the bitrate of the audio represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing a bitrate of the
		///    audio represented by the current instance.
		/// </value>
		public int AudioBitrate {
			get {
				return (int) Math.Round (
					average_bytes_per_second * 8d / 1000d);
			}
		}
		
		/// <summary>
		///    Gets the sample rate of the audio represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the sample rate of
		///    the audio represented by the current instance.
		/// </value>
		public int AudioSampleRate {
			get {return (int) samples_per_second;}
		}
		
		/// <summary>
		///    Gets the number of channels in the audio represented by
		///    the current instance.
		/// </summary>
		/// <value>
		///    A <see cref="int" /> value containing the number of
		///    channels in the audio represented by the current
		///    instance.
		/// </value>
		public int AudioChannels {
			get {return channels;}
		}
		
		/// <summary>
		///    Gets the types of media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    Always <see cref="MediaTypes.Audio" />.
		/// </value>
		public MediaTypes MediaTypes {
			get {return MediaTypes.Audio;}
		}
		
		/// <summary>
		///    Gets the duration of the media represented by the current
		///    instance.
		/// </summary>
		/// <value>
		///    Always <see cref="TimeSpan.Zero" />.
		/// </value>
		public TimeSpan Duration {
			get {return TimeSpan.Zero;}
		}
		
		/// <summary>
		///    Gets a text description of the media represented by the
		///    current instance.
		/// </summary>
		/// <value>
		///    A <see cref="string" /> object containing a description
		///    of the media represented by the current instance.
		/// </value>
		public string Description {
			get {
				switch (FormatTag)
				{
				case 0x0000:
					return "Unknown Wave Format";
				case 0x0001:
					return "PCM Audio";
				case 0x0002:
					return "Microsoft Adaptive PCM Audio";
				case 0x0003:
					return "PCM Audio in IEEE floating-point format";
				case 0x0004:
					return "Compaq VSELP Audio";
				case 0x0005:
					return "IBM CVSD Audio";
				case 0x0006:
					return "Microsoft ALAW Audio";
				case 0x0007:
					return "Microsoft MULAW Audio";
				case 0x0008:
					return "Microsoft DTS Audio";
				case 0x0009:
					return "Microsoft DRM Encrypted Audio";
				case 0x000A:
					return "Microsoft Speech Audio";
				case 0x000B:
					return "Microsoft Windows Media RT Voice Audio";
				case 0x0010:
					return "OKI ADPCM Audio";
				case 0x0011:
					return "Intel ADPCM Audio";
				case 0x0012:
					return "VideoLogic ADPCM Audio";
				case 0x0013:
					return "Sierra ADPCM Audio";
				case 0x0014:
					return "Antex ADPCM Audio";
				case 0x0015:
					return "DSP DIGISTD Audio";
				case 0x0016:
					return "DSP DIGIFIX Audio";
				case 0x0017:
					return "Dialogic OKI ADPCM Audio";
				case 0x0018:
					return "Media Vision ADPCM Audio for Jazz 16";
				case 0x0019:
					return "Hewlett-Packard CU Audio";
				case 0x001A:
					return "Hewlett-Packard Dynamic Voice Audio";
				case 0x0020:
					return "Yamaha ADPCM Audio";
				case 0x0021:
					return "Speech Compression Audio";
				case 0x0022:
					return "DSP Group True Speech Audio";
				case 0x0023:
					return "Echo Speech Audio";
				case 0x0024:
					return "Ahead AF36 Audio";
				case 0x0025:
					return "Audio Processing Technology Audio";
				case 0x0026:
					return "Ahead AF10 Audio";
				case 0x0027:
					return "Aculab Prosody CTI Speech Card Audio";
				case 0x0028:
					return "Merging Technologies LRC Audio";
				case 0x0030:
					return "Dolby AC2 Audio";
				case 0x0031:
					return "Microsoft GSM6.10 Audio";
				case 0x0032:
					return "Microsoft MSN Audio";
				case 0x0033:
					return "Antex ADPCME Audio";
				case 0x0034:
					return "Control Resources VQLPC";
				case 0x0035:
					return "DSP REAL Audio";
				case 0x0036:
					return "DSP ADPCM Audio";
				case 0x0037:
					return "Control Resources CR10 Audio";
				case 0x0038:
					return "Natural MicroSystems VBXADPCM Audio";
				case 0x0039:
					return "Roland RDAC Proprietary Audio Format";
				case 0x003A:
					return "Echo Speech Proprietary Audio Compression Format";
				case 0x003B:
					return "Rockwell ADPCM Audio";
				case 0x003C:
					return "Rockwell DIGITALK Audio";
				case 0x003D:
					return "Xebec Proprietary Audio Compression Format";
				case 0x0040:
					return "Antex G721 ADPCM Audio";
				case 0x0041:
					return "Antex G728 CELP Audio";
				case 0x0042:
					return "Microsoft MSG723 Audio";
				case 0x0043:
					return "Microsoft MSG723.1 Audio";
				case 0x0044:
					return "Microsoft MSG729 Audio";
				case 0x0045:
					return "Microsoft SPG726 Audio";
				case 0x0050:
					return "Microsoft MPEG Audio";
				case 0x0052:
					return "InSoft RT24 Audio";
				case 0x0053:
					return "InSoft PAC Audio";
				case 0x0055:
					return "ISO/MPEG Layer 3 Audio";
				case 0x0059:
					return "Lucent G723 Audio";
				case 0x0060:
					return "Cirrus Logic Audio";
				case 0x0061:
					return "ESS Technology PCM Audio";
				case 0x0062:
					return "Voxware Audio";
				case 0x0063:
					return "Canopus ATRAC Audio";
				case 0x0064:
					return "APICOM G726 ADPCM Audio";
				case 0x0065:
					return "APICOM G722 ADPCM Audio";
				case 0x0067:
					return "Microsoft DSAT Display Audio";
				case 0x0069:
					return "Voxware Byte Aligned Audio";
				case 0x0070:
					return "Voxware AC8 Audio";
				case 0x0071:
					return "Voxware AC10 Audio";
				case 0x0072:
					return "Voxware AC16 Audio";
				case 0x0073:
					return "Voxware AC20 Audio";
				case 0x0074:
					return "Voxware RT24 Audio";
				case 0x0075:
					return "Voxware RT29 Audio";
				case 0x0076:
					return "Voxware RT29HW Audio";
				case 0x0077:
					return "Voxware VR12 Audio";
				case 0x0078:
					return "Voxware VR18 Audio";
				case 0x0079:
					return "Voxware TQ40 Audio";
				case 0x007A:
					return "Voxware SC3 Audio";
				case 0x007B:
					return "Voxware SC3 Audio";
				case 0x0080:
					return "SoftSound Audio";
				case 0x0081:
					return "Voxware TQ60 Audio";
				case 0x0082:
					return "Microsoft RT24 Audio";
				case 0x0083:
					return "AT&T G729A Audio";
				case 0x0084:
					return "Motion Pixels MVI2 Audio";
				case 0x0085:
					return "Datafusion Systems G726 Audio";
				case 0x0086:
					return "Datafusion Systems G610 Audio";
				case 0x0088:
					return "Iterated Systems Audio";
				case 0x0089:
					return "OnLive! Audio";
				case 0x008A:
					return "Multitude FT SX20 Audio";
				case 0x008B:
					return "InfoCom ITS ACM G721 Audio";
				case 0x008C:
					return "Convedia G729 Audio";
				case 0x008D:
					return "Congruency Audio";
				case 0x0091:
					return "Siemens Business Communications 24 Audio";
				case 0x0092:
					return "Sonic Foundary Dolby AC3 Audio";
				case 0x0093:
					return "MediaSonic G723 Audio";
				case 0x0094:
					return "Aculab Prosody CTI Speech Card Audio";
				case 0x0097:
					return "ZyXEL ADPCM";
				case 0x0098:
					return "Philips Speech Processing LPCBB Audio";
				case 0x0099:
					return "Studer Professional PACKED Audio";
				case 0x00A0:
					return "Malden Electronics Phony Talk Audio";
				case 0x00A1:
					return "Racal Recorder GSM Audio";
				case 0x00A2:
					return "Racal Recorder G720.a Audio";
				case 0x00A3:
					return "Racal G723.1 Audio";
				case 0x00A4:
					return "Racal Tetra ACELP Audio";
				case 0x00B0:
					return "NEC AAC Audio";
				case 0x0100:
					return "Rhetorex ADPCM Audio";
				case 0x0101:
					return "BeCubed IRAT Audio";
				case 0x0111:
					return "Vivo G723 Audio";
				case 0x0112:
					return "Vivo Siren Audio";
				case 0x0120:
					return "Philips Speach Processing CELP Audio";
				case 0x0121:
					return "Philips Speach Processing GRUNDIG Audio";
				case 0x0123:
					return "Digital Equipment Corporation G723 Audio";
				case 0x0125:
					return "Sanyo LD-ADPCM Audio";
				case 0x0130:
					return "Sipro Lab ACELPNET Audio";
				case 0x0131:
					return "Sipro Lab ACELP4800 Audio";
				case 0x0132:
					return "Sipro Lab ACELP8v3 Audio";
				case 0x0133:
					return "Sipro Lab G729 Audio";
				case 0x0134:
					return "Sipro Lab G729A Audio";
				case 0x0135:
					return "Sipro Lab KELVIN Audio";
				case 0x0136:
					return "VoiceAge AMR Audio";
				case 0x0140:
					return "Dictaphone G726 ADPCM Audio";
				case 0x0141:
					return "Dictaphone CELP68 Audio";
				case 0x0142:
					return "Dictaphone CELP54 Audio";
				case 0x0150:
					return "QUALCOMM Pure Voice Audio";
				case 0x0151:
					return "QUALCOMM Half Rate Audio";
				case 0x0155:
					return "Ring Zero TUBGSM Audio";
				case 0x0160:
					return "Microsoft WMA1 Audio";
				case 0x0161:	
					return "Microsoft WMA2 Audio";
				case 0x0162:
					return "Microsoft Multichannel WMA Audio";
				case 0x0163:
					return "Microsoft Lossless WMA Audio";
				case 0x0170:
					return "Unisys NAP ADPCM Audio";
				case 0x0171:
					return "Unisys NAP ULAW Audio";
				case 0x0172:
					return "Unisys NAP ALAW Audio";
				case 0x0173:
					return "Unisys NAP 16K Audio";
				case 0X0174:
					return "SysCom ACM SYC008 Audio";
				case 0x0175:
					return "SysCom ACM SYC701 G726L Audio";
				case 0x0176:
					return "SysCom ACM SYC701 CELP54 Audio";
				case 0x0177:
					return "SysCom ACM SYC701 CELP68 Audio";
				case 0x0178:
					return "Knowledge Adventure ADPCM Audio";
				case 0x0180:
					return "MPEG2 AAC Audio";
				case 0x0190:
					return "Digital Theater Systems DTS DS Audio";
				case 0x1979:
					return "Innings ADPCM Audio";
				case 0x0200:
					return "Creative ADPCM Audio";
				case 0x0202:
					return "Creative FastSpeech8 Audio";
				case 0x0203:
					return "Creative FastSpeech10 Audio";
				case 0x0210:
					return "UHER ADPCM Audio";
				case 0x0220:
					return "Quarterdeck Audio";
				case 0x0230:
					return "I-Link VC Audio";
				case 0x0240:
					return "Aureal RAW SPORT Audio";
				case 0x0250:
					return "Interactive Prodcuts HSX Audio";
				case 0x0251:
					return "Interactive Products RPELP Audio";
				case 0x0260:
					return "Consistens Software CS2 Audio";
				case 0x0270:
					return "Sony SCX Audio";
				case 0x0271:
					return "Sony SCY Audio";
				case 0x0272:
					return "Sony ATRAC3 Audio";
				case 0x0273:
					return "Sony SPC Audio";
				case 0x0280:
					return "Telum Audio";
				case 0x0281:
					return "Telum IA Audio";
				case 0x0285:
					return "Norcom Voice Systems ADPCM Audio";
				case 0x0300:
					return "Fujitsu FM TOWNS SND Audio";
				case 0x0301:
				case 0x0302:
				case 0x0303:
				case 0x0304:
				case 0x0305:
				case 0x0306:
				case 0x0307:
				case 0x0308:
					return "Unknown Fujitsu Audio";
				case 0x0350:
					return "Micronas Semiconductors Development Audio";
				case 0x0351:
					return "Micronas Semiconductors CELP833 Audio";
				case 0x0400:
					return "Brooktree Digital Audio";
				case 0x0450:
					return "QDesign Audio";
				case 0x0680:
					return "AT&T VME VMPCM Audio";
				case 0x0681:
					return "AT&T TPC Audio";
				case 0x1000:
					return "Ing. C. Olivetti & C., S.p.A. GSM Audio";
				case 0x1001:
					return "Ing. C. Olivetti & C., S.p.A. ADPCM Audio";
				case 0x1002:
					return "Ing. C. Olivetti & C., S.p.A. CELP Audio";
				case 0x1003:
					return "Ing. C. Olivetti & C., S.p.A. SBC Audio";
				case 0x1004:
					return "Ing. C. Olivetti & C., S.p.A. OPR Audio";
				case 0x1100:
					return "Lernout & Hauspie Audio";
				case 0X1101:
					return "Lernout & Hauspie CELP Audio";
				case 0X1102:
					return "Lernout & Hauspie SB8 Audio";
				case 0X1103:
					return "Lernout & Hauspie SB12 Audio";
				case 0X1104:
					return "Lernout & Hauspie SB16 Audio";
				case 0x1400:
					return "Norris Audio";
				case 0x1500:
					return "AT&T Soundspace Musicompress Audio";
				case 0x1971:
					return "Sonic Foundry Lossless Audio";
				case 0x2000:
					return "FAST Multimedia DVM Audio";
				case 0x4143:
					return "Divio AAC";
				case 0x4201:
					return "Nokia Adaptive Multirate Audio";
				case 0x4243:
					return "Divio G726 Audio";
				case 0x7000:
					return "3Com NBX Audio";
				case 0x7A21:
					return "Microsoft Adaptive Multirate Audio";
				case 0x7A22:
					return "Microsoft Adaptive Multirate Audio with silence detection";
				case 0xA100:
					return "Comverse Infosys G723 1 Audio";
				case 0xA101:
					return "Comverse Infosys AVQSBC Audio";
				case 0xA102:
					return "Comverse Infosys OLDSBC Audio";
				case 0xA103:
					return "Symbol Technology G729A Audio";
				case 0xA104:
					return "VoiceAge AMR WB Audio";
				case 0xA105:
					return "Ingenient G726 Audio";
				case 0xA106:
					return "ISO/MPEG-4 Advanced Audio Coding";
				case 0xA107:
					return "Encore G726 Audio";
				default:
					return "Unknown Audio (" + FormatTag + ")";
				}
			}
		}
		
#endregion
		
		
		
#region IEquatable
		
		/// <summary>
		///    Generates a hash code for the current instance.
		/// </summary>
		/// <returns>
		///    A <see cref="int" /> value containing the hash code for
		///    the current instance.
		/// </returns>
		public override int GetHashCode ()
		{
			unchecked {
				return (int) (format_tag ^ channels ^
					samples_per_second ^
					average_bytes_per_second ^
					bits_per_sample);
			}
		}
		
		/// <summary>
		///    Checks whether or not the current instance is equal to
		///    another object.
		/// </summary>
		/// <param name="other">
		///    A <see cref="object" /> to compare to the current
		///    instance.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance is equal to <paramref name="other" />.
		/// </returns>
		/// <seealso cref="M:System.IEquatable`1.Equals" />
		public override bool Equals (object other)
		{
			if (!(other is WaveFormatEx))
				return false;
			
			return Equals ((WaveFormatEx) other);
		}
		
		/// <summary>
		///    Checks whether or not the current instance is equal to
		///    another instance of <see cref="WaveFormatEx" />.
		/// </summary>
		/// <param name="other">
		///    A <see cref="WaveFormatEx" /> object to compare to the
		///    current instance.
		/// </param>
		/// <returns>
		///    A <see cref="bool" /> value indicating whether or not the
		///    current instance is equal to <paramref name="other" />.
		/// </returns>
		/// <seealso cref="M:System.IEquatable`1.Equals" />
		public bool Equals (WaveFormatEx other)
		{
			return format_tag == other.format_tag &&
				channels == other.channels &&
				samples_per_second == other.samples_per_second &&
				average_bytes_per_second == other.average_bytes_per_second &&
				bits_per_sample == other.bits_per_sample;
		}
		
		/// <summary>
		///    Gets whether or not two instances of <see
		///    cref="WaveFormatEx" /> are equal to eachother.
		/// </summary>
		/// <param name="first">
		///    A <see cref="WaveFormatEx" /> object to compare.
		/// </param>
		/// <param name="second">
		///    A <see cref="WaveFormatEx" /> object to compare.
		/// </param>
		/// <returns>
		///    <see langword="true" /> if <paramref name="first" /> is
		///    equal to <paramref name="second" />. Otherwise, <see
		///    langword="false" />.
		/// </returns>
		public static bool operator == (WaveFormatEx first,
		                                WaveFormatEx second)
		{
			return first.Equals (second);
		}
		
		/// <summary>
		///    Gets whether or not two instances of <see
		///    cref="WaveFormatEx" /> differ.
		/// </summary>
		/// <param name="first">
		///    A <see cref="WaveFormatEx" /> object to compare.
		/// </param>
		/// <param name="second">
		///    A <see cref="WaveFormatEx" /> object to compare.
		/// </param>
		/// <returns>
		///    <see langword="true" /> if <paramref name="first" /> is
		///    unequal to <paramref name="second" />. Otherwise, <see
		///    langword="false" />.
		/// </returns>
		public static bool operator != (WaveFormatEx first,
		                                WaveFormatEx second)
		{
			return !first.Equals (second);
		}
#endregion
	}
}
