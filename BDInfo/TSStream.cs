//============================================================================
// BDInfo - Blu-ray Video and Audio Analysis Tool
// Copyright © 2010 Cinema Squid
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//=============================================================================

using System;
using System.Collections.Generic;

namespace BDInfo
{
    public enum TSStreamType : byte
    {
        Unknown = 0,
        MPEG1_VIDEO = 0x01,
        MPEG2_VIDEO = 0x02,
        AVC_VIDEO = 0x1b,
        MVC_VIDEO = 0x20,
        VC1_VIDEO = 0xea,
        MPEG1_AUDIO = 0x03,
        MPEG2_AUDIO = 0x04,
        LPCM_AUDIO = 0x80,
        AC3_AUDIO = 0x81,
        AC3_PLUS_AUDIO = 0x84,
        AC3_PLUS_SECONDARY_AUDIO = 0xA1,
        AC3_TRUE_HD_AUDIO = 0x83,
        DTS_AUDIO = 0x82,
        DTS_HD_AUDIO = 0x85,
        DTS_HD_SECONDARY_AUDIO = 0xA2,
        DTS_HD_MASTER_AUDIO = 0x86,
        PRESENTATION_GRAPHICS = 0x90,
        INTERACTIVE_GRAPHICS = 0x91,
        SUBTITLE = 0x92
    }

    public enum TSVideoFormat : byte
    {
        Unknown = 0,
        VIDEOFORMAT_480i = 1,
        VIDEOFORMAT_576i = 2,
        VIDEOFORMAT_480p = 3,
        VIDEOFORMAT_1080i = 4,
        VIDEOFORMAT_720p = 5,
        VIDEOFORMAT_1080p = 6,
        VIDEOFORMAT_576p = 7,
    }

    public enum TSFrameRate : byte
    {
        Unknown = 0,
        FRAMERATE_23_976 = 1,
        FRAMERATE_24 = 2,
        FRAMERATE_25 = 3,
        FRAMERATE_29_97 = 4,
        FRAMERATE_50 = 6,
        FRAMERATE_59_94 = 7
    }

    public enum TSChannelLayout : byte
    {
        Unknown = 0,
        CHANNELLAYOUT_MONO = 1,
        CHANNELLAYOUT_STEREO = 3,
        CHANNELLAYOUT_MULTI = 6,
        CHANNELLAYOUT_COMBO = 12
    }

    public enum TSSampleRate : byte
    {
        Unknown = 0,
        SAMPLERATE_48 = 1,
        SAMPLERATE_96 = 4,
        SAMPLERATE_192 = 5,
        SAMPLERATE_48_192 = 12,
        SAMPLERATE_48_96 = 14
    }

    public enum TSAspectRatio
    {
        Unknown = 0,
        ASPECT_4_3 = 2,
        ASPECT_16_9 = 3,
        ASPECT_2_21 = 4
    }

    public class TSDescriptor
    {
        public byte Name;
        public byte[] Value;

        public TSDescriptor(byte name, byte length)
        {
            Name = name;
            Value = new byte[length];
        }

        public TSDescriptor Clone()
        {
            TSDescriptor descriptor = 
                new TSDescriptor(Name, (byte)Value.Length);
            Value.CopyTo(descriptor.Value, 0);
            return descriptor;
        }
    }

    public abstract class TSStream
    {
        public TSStream()
        {
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", CodecShortName, PID);
        }

        public ushort PID;
        public TSStreamType StreamType;
        public List<TSDescriptor> Descriptors = null;
        public long BitRate = 0;
        public long ActiveBitRate = 0;
        public bool IsVBR = false;
        public bool IsInitialized = false;
        public string LanguageName;
        public bool IsHidden = false;

        public ulong PayloadBytes = 0;
        public ulong PacketCount = 0;
        public double PacketSeconds = 0;
        public int AngleIndex = 0;

        public ulong PacketSize
        {
            get
            {
                return PacketCount * 192;
            }
        }

        private string _LanguageCode;
        public string LanguageCode
        {
            get 
            {
                return _LanguageCode; 
            }
            set 
            {
                _LanguageCode = value;
                LanguageName = LanguageCodes.GetName(value);
            } 
        }

        public bool IsVideoStream
        {
            get
            {
                switch (StreamType)
                {
                    case TSStreamType.MPEG1_VIDEO:
                    case TSStreamType.MPEG2_VIDEO:
                    case TSStreamType.AVC_VIDEO:
                    case TSStreamType.MVC_VIDEO:
                    case TSStreamType.VC1_VIDEO:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public bool IsAudioStream
        {
            get
            {
                switch (StreamType)
                {
                    case TSStreamType.MPEG1_AUDIO:
                    case TSStreamType.MPEG2_AUDIO:
                    case TSStreamType.LPCM_AUDIO:
                    case TSStreamType.AC3_AUDIO:
                    case TSStreamType.AC3_PLUS_AUDIO:
                    case TSStreamType.AC3_PLUS_SECONDARY_AUDIO:
                    case TSStreamType.AC3_TRUE_HD_AUDIO:
                    case TSStreamType.DTS_AUDIO:
                    case TSStreamType.DTS_HD_AUDIO:
                    case TSStreamType.DTS_HD_SECONDARY_AUDIO:
                    case TSStreamType.DTS_HD_MASTER_AUDIO:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public bool IsGraphicsStream
        {
            get
            {
                switch (StreamType)
                {
                    case TSStreamType.PRESENTATION_GRAPHICS:
                    case TSStreamType.INTERACTIVE_GRAPHICS:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public bool IsTextStream
        {
            get
            {
                switch (StreamType)
                {
                    case TSStreamType.SUBTITLE:
                        return true;

                    default:
                        return false;
                }
            }
        }

        public string CodecName
        {
            get
            {
                switch (StreamType)
                {
                    case TSStreamType.MPEG1_VIDEO:
                        return "MPEG-1 Video";
                    case TSStreamType.MPEG2_VIDEO:
                        return "MPEG-2 Video";
                    case TSStreamType.AVC_VIDEO:
                        return "MPEG-4 AVC Video";
                    case TSStreamType.MVC_VIDEO:
                        return "MPEG-4 MVC Video";
                    case TSStreamType.VC1_VIDEO:
                        return "VC-1 Video";
                    case TSStreamType.MPEG1_AUDIO:
                        return "MP1 Audio";
                    case TSStreamType.MPEG2_AUDIO:
                        return "MP2 Audio";
                    case TSStreamType.LPCM_AUDIO:
                        return "LPCM Audio";
                    case TSStreamType.AC3_AUDIO:
                        if (((TSAudioStream)this).AudioMode == TSAudioMode.Extended)
                            return "Dolby Digital EX Audio";
                        else
                            return "Dolby Digital Audio";
                    case TSStreamType.AC3_PLUS_AUDIO:
                    case TSStreamType.AC3_PLUS_SECONDARY_AUDIO:
                        return "Dolby Digital Plus Audio";
                    case TSStreamType.AC3_TRUE_HD_AUDIO:
                        return "Dolby TrueHD Audio";
                    case TSStreamType.DTS_AUDIO:
                        if (((TSAudioStream)this).AudioMode == TSAudioMode.Extended)
                            return "DTS-ES Audio";
                        else
                            return "DTS Audio";
                    case TSStreamType.DTS_HD_AUDIO:
                        return "DTS-HD High-Res Audio";
                    case TSStreamType.DTS_HD_SECONDARY_AUDIO:
                        return "DTS Express";
                    case TSStreamType.DTS_HD_MASTER_AUDIO:
                        return "DTS-HD Master Audio";
                    case TSStreamType.PRESENTATION_GRAPHICS:
                        return "Presentation Graphics";
                    case TSStreamType.INTERACTIVE_GRAPHICS:
                        return "Interactive Graphics";
                    case TSStreamType.SUBTITLE:
                        return "Subtitle";
                    default:
                        return "UNKNOWN";
                }
            }
        }

        public string CodecAltName
        {
            get
            {
                switch (StreamType)
                {
                    case TSStreamType.MPEG1_VIDEO:
                        return "MPEG-1";
                    case TSStreamType.MPEG2_VIDEO:
                        return "MPEG-2";
                    case TSStreamType.AVC_VIDEO:
                        return "AVC";
                    case TSStreamType.MVC_VIDEO:
                        return "MVC";
                    case TSStreamType.VC1_VIDEO:
                        return "VC-1";
                    case TSStreamType.MPEG1_AUDIO:
                        return "MP1";
                    case TSStreamType.MPEG2_AUDIO:
                        return "MP2";
                    case TSStreamType.LPCM_AUDIO:
                        return "LPCM";
                    case TSStreamType.AC3_AUDIO:
                        return "DD AC3";
                    case TSStreamType.AC3_PLUS_AUDIO:
                    case TSStreamType.AC3_PLUS_SECONDARY_AUDIO:
                        return "DD AC3+";
                    case TSStreamType.AC3_TRUE_HD_AUDIO:
                        return "Dolby TrueHD";
                    case TSStreamType.DTS_AUDIO:
                        return "DTS";
                    case TSStreamType.DTS_HD_AUDIO:
                        return "DTS-HD Hi-Res";
                    case TSStreamType.DTS_HD_SECONDARY_AUDIO:
                        return "DTS Express";
                    case TSStreamType.DTS_HD_MASTER_AUDIO:
                        return "DTS-HD Master";
                    case TSStreamType.PRESENTATION_GRAPHICS:
                        return "PGS";
                    case TSStreamType.INTERACTIVE_GRAPHICS:
                        return "IGS";
                    case TSStreamType.SUBTITLE:
                        return "SUB";
                    default:
                        return "UNKNOWN";
                }
            }
        }

        public string CodecShortName
        {
            get
            {
                switch (StreamType)
                {
                    case TSStreamType.MPEG1_VIDEO:
                        return "MPEG-1";
                    case TSStreamType.MPEG2_VIDEO:
                        return "MPEG-2";
                    case TSStreamType.AVC_VIDEO:
                        return "AVC";
                    case TSStreamType.MVC_VIDEO:
                        return "MVC";
                    case TSStreamType.VC1_VIDEO:
                        return "VC-1";
                    case TSStreamType.MPEG1_AUDIO:
                        return "MP1";
                    case TSStreamType.MPEG2_AUDIO:
                        return "MP2";
                    case TSStreamType.LPCM_AUDIO:
                        return "LPCM";
                    case TSStreamType.AC3_AUDIO:
                        if (((TSAudioStream)this).AudioMode == TSAudioMode.Extended)
                            return "AC3-EX";
                        else
                            return "AC3";
                    case TSStreamType.AC3_PLUS_AUDIO:
                    case TSStreamType.AC3_PLUS_SECONDARY_AUDIO:
                        return "AC3+";
                    case TSStreamType.AC3_TRUE_HD_AUDIO:
                        return "TrueHD";
                    case TSStreamType.DTS_AUDIO:
                        if (((TSAudioStream)this).AudioMode == TSAudioMode.Extended)
                            return "DTS-ES";
                        else
                            return "DTS";
                    case TSStreamType.DTS_HD_AUDIO:
                        return "DTS-HD HR";
                    case TSStreamType.DTS_HD_SECONDARY_AUDIO:
                        return "DTS Express";
                    case TSStreamType.DTS_HD_MASTER_AUDIO:
                        return "DTS-HD MA";
                    case TSStreamType.PRESENTATION_GRAPHICS:
                        return "PGS";
                    case TSStreamType.INTERACTIVE_GRAPHICS:
                        return "IGS";
                    case TSStreamType.SUBTITLE:
                        return "SUB";
                    default:
                        return "UNKNOWN";
                }
            }
        }

        public virtual string Description
        {
            get
            {
                return "";
            }
        }

        public abstract TSStream Clone();
        
        protected void CopyTo(TSStream stream)
        {
            stream.PID = PID;
            stream.StreamType = StreamType;
            stream.IsVBR = IsVBR;
            stream.BitRate = BitRate;
            stream.IsInitialized = IsInitialized;
            stream.LanguageCode = _LanguageCode;
            if (Descriptors != null)
            {
                stream.Descriptors = new List<TSDescriptor>();
                foreach (TSDescriptor descriptor in Descriptors)
                {
                    stream.Descriptors.Add(descriptor.Clone());
                }
            }
        }
    }

    public class TSVideoStream : TSStream
    {
        public TSVideoStream()
        {
        }

        public int Width;
        public int Height;
        public bool IsInterlaced;        
        public int FrameRateEnumerator;
        public int FrameRateDenominator;
        public TSAspectRatio AspectRatio;
        public string EncodingProfile;

        private TSVideoFormat _VideoFormat;
        public TSVideoFormat VideoFormat
        {
            get
            {
                return _VideoFormat;
            }
            set
            {
                _VideoFormat = value;
                switch (value)
                {
                    case TSVideoFormat.VIDEOFORMAT_480i:
                        Height = 480;
                        IsInterlaced = true;
                        break;
                    case TSVideoFormat.VIDEOFORMAT_480p:
                        Height = 480;
                        IsInterlaced = false;
                        break;
                    case TSVideoFormat.VIDEOFORMAT_576i:
                        Height = 576;
                        IsInterlaced = true;
                        break;
                    case TSVideoFormat.VIDEOFORMAT_576p:
                        Height = 576;
                        IsInterlaced = false;
                        break;
                    case TSVideoFormat.VIDEOFORMAT_720p:
                        Height = 720;
                        IsInterlaced = false;
                        break;
                    case TSVideoFormat.VIDEOFORMAT_1080i:
                        Height = 1080;
                        IsInterlaced = true;
                        break;
                    case TSVideoFormat.VIDEOFORMAT_1080p:
                        Height = 1080;
                        IsInterlaced = false;
                        break;
                }
            }
        }

        private TSFrameRate _FrameRate;
        public TSFrameRate FrameRate
        {
            get
            {
                return _FrameRate;
            }
            set
            {
                _FrameRate = value;
                switch (value)
                {
                    case TSFrameRate.FRAMERATE_23_976:
                        FrameRateEnumerator = 24000;
                        FrameRateDenominator = 1001;
                        break;
                    case TSFrameRate.FRAMERATE_24:
                        FrameRateEnumerator = 24000;
                        FrameRateDenominator = 1000;
                        break;
                    case TSFrameRate.FRAMERATE_25:
                        FrameRateEnumerator = 25000;
                        FrameRateDenominator = 1000;
                        break;
                    case TSFrameRate.FRAMERATE_29_97:
                        FrameRateEnumerator = 30000;
                        FrameRateDenominator = 1001;
                        break;
                    case TSFrameRate.FRAMERATE_50:
                        FrameRateEnumerator = 50000;
                        FrameRateDenominator = 1000;
                        break;
                    case TSFrameRate.FRAMERATE_59_94:
                        FrameRateEnumerator = 60000;
                        FrameRateDenominator = 1001;
                        break;
                }
            }
        }

        public override string Description
        {
            get
            {
                string description = "";

                if (Height > 0)
                {
                    description += string.Format("{0:D}{1} / ",
                        Height,
                        IsInterlaced ? "i" : "p");
                }
                if (FrameRateEnumerator > 0 &&
                    FrameRateDenominator > 0)
                {
                    if (FrameRateEnumerator % FrameRateDenominator == 0)
                    {
                        description += string.Format("{0:D} fps / ",
                            FrameRateEnumerator / FrameRateDenominator);
                    }
                    else
                    {
                        description += string.Format("{0:F3} fps / ",
                            (double)FrameRateEnumerator / FrameRateDenominator);
                    }

                }
                if (AspectRatio == TSAspectRatio.ASPECT_4_3)
                {
                    description += "4:3 / ";
                }
                else if (AspectRatio == TSAspectRatio.ASPECT_16_9)
                {
                    description += "16:9 / ";
                }
                if (EncodingProfile != null)
                {
                    description += EncodingProfile + " / ";
                }
                if (description.EndsWith(" / "))
                {
                    description = description.Substring(0, description.Length - 3);
                }
                return description;
            }
        }

        public override TSStream Clone()
        {
            TSVideoStream stream = new TSVideoStream();
            CopyTo(stream);

            stream.VideoFormat = _VideoFormat;
            stream.FrameRate = _FrameRate;
            stream.Width = Width;
            stream.Height = Height;
            stream.IsInterlaced = IsInterlaced;        
            stream.FrameRateEnumerator = FrameRateEnumerator;
            stream.FrameRateDenominator = FrameRateDenominator;
            stream.AspectRatio = AspectRatio;
            stream.EncodingProfile = EncodingProfile;

            return stream;
        }
    }

    public enum TSAudioMode
    {
        Unknown,
        DualMono,
        Stereo,
        Surround,
        Extended
    }

    public class TSAudioStream : TSStream
    {
        public TSAudioStream()
        {
        }

        public int SampleRate;
        public int ChannelCount;
        public int BitDepth;
        public int LFE;
        public int DialNorm;
        public TSAudioMode AudioMode;
        public TSAudioStream CoreStream;
        public TSChannelLayout ChannelLayout;

        public static int ConvertSampleRate(
            TSSampleRate sampleRate)
        {
            switch (sampleRate)
            {
                case TSSampleRate.SAMPLERATE_48:
                    return 48000;

                case TSSampleRate.SAMPLERATE_96:
                case TSSampleRate.SAMPLERATE_48_96:
                    return 96000;

                case TSSampleRate.SAMPLERATE_192:
                case TSSampleRate.SAMPLERATE_48_192:
                    return 192000;
            }
            return 0;
        }

        public string ChannelDescription
        {
            get
            {
                if (ChannelLayout == TSChannelLayout.CHANNELLAYOUT_MONO &&
                    ChannelCount == 2)
                {
                }

                string description = "";
                if (ChannelCount > 0)
                {
                    description += string.Format(
                        "{0:D}.{1:D}",
                        ChannelCount, LFE);
                }
                else
                {
                    switch (ChannelLayout)
                    {
                        case TSChannelLayout.CHANNELLAYOUT_MONO:
                            description += "1.0";
                            break;
                        case TSChannelLayout.CHANNELLAYOUT_STEREO:
                            description += "2.0";
                            break;
                        case TSChannelLayout.CHANNELLAYOUT_MULTI:
                            description += "5.1";
                            break;
                    }
                }
                if (AudioMode == TSAudioMode.Extended)
                {
                    if (StreamType == TSStreamType.AC3_AUDIO)
                    {
                        description += "-EX";
                    }
                    if (StreamType == TSStreamType.DTS_AUDIO ||
                        StreamType == TSStreamType.DTS_HD_AUDIO ||
                        StreamType == TSStreamType.DTS_HD_MASTER_AUDIO)
                    {
                        description += "-ES";
                    }
                }
                return description;
            }
        }

        public override string Description
        {
            get
            {
                string description = ChannelDescription;

                if (SampleRate > 0)
                {
                    description += string.Format(
                        " / {0:D} kHz", SampleRate / 1000);
                }
                if (BitRate > 0)
                {
                    description += string.Format(
                        " / {0:D} kbps", (uint)Math.Round((double)BitRate / 1000));
                }
                if (BitDepth > 0)
                {
                    description += string.Format(
                        " / {0:D}-bit", BitDepth);
                }
                if (DialNorm != 0)
                {
                    description += string.Format(
                        " / DN {0}dB", DialNorm);
                }
                if (ChannelCount == 2)
                {
                    switch (AudioMode)
                    {
                        case TSAudioMode.DualMono:
                            description += " / Dual Mono";
                            break;

                        case TSAudioMode.Surround:
                            description += " / Dolby Surround";
                            break;
                    }
                }
                if (description.EndsWith(" / "))
                {
                    description = description.Substring(0, description.Length - 3);
                }
                if (CoreStream != null)
                {
                    string codec = "";
                    switch (CoreStream.StreamType)
                    {
                        case TSStreamType.AC3_AUDIO:
                            codec = "AC3 Embedded";
                            break;
                        case TSStreamType.DTS_AUDIO:
                            codec = "DTS Core";
                            break;
                    }
                    description += string.Format(
                        " ({0}: {1})",
                        codec,
                        CoreStream.Description);
                }
                return description;
            }
        }

        public override TSStream Clone()
        {
            TSAudioStream stream = new TSAudioStream();
            CopyTo(stream);

            stream.SampleRate = SampleRate;
            stream.ChannelLayout = ChannelLayout;
            stream.ChannelCount = ChannelCount;
            stream.BitDepth = BitDepth;
            stream.LFE = LFE;
            stream.DialNorm = DialNorm;
            stream.AudioMode = AudioMode;
            if (CoreStream != null)
            {
                stream.CoreStream = (TSAudioStream)CoreStream.Clone();
            }

            return stream;
        }
    }

    public class TSGraphicsStream : TSStream
    {
        public TSGraphicsStream()
        {
            IsVBR = true;
            IsInitialized = true;
        }

        public override TSStream Clone()
        {
            TSGraphicsStream stream = new TSGraphicsStream();
            CopyTo(stream);
            return stream;
        }
    }

    public class TSTextStream : TSStream
    {
        public TSTextStream()
        {
            IsVBR = true;
            IsInitialized = true;
        }

        public override TSStream Clone()
        {
            TSTextStream stream = new TSTextStream();
            CopyTo(stream);
            return stream;
        }
    }
}
