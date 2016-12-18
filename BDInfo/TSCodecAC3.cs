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

#undef DEBUG
using System.IO;

namespace BDInfo
{
    public abstract class TSCodecAC3
    {
        private static byte[] eac3_blocks =  new byte[] { 1, 2, 3, 6 };

        public static void Scan(
            TSAudioStream stream,
            TSStreamBuffer buffer,
            ref string tag)
        {
            if (stream.IsInitialized) return;

            byte[] sync = buffer.ReadBytes(2);
            if (sync == null ||
                sync[0] != 0x0B ||
                sync[1] != 0x77)
            {
                return;
            }

            int sr_code = 0;
            int frame_size = 0;
            int frame_size_code = 0;
            int channel_mode = 0;
            int lfe_on = 0;
            int dial_norm = 0;
            int num_blocks = 0;

            byte[] hdr = buffer.ReadBytes(4);
            int bsid = (hdr[3] & 0xF8) >> 3;
            buffer.Seek(-4, SeekOrigin.Current);
            if (bsid <= 10)
            {
                byte[] crc = buffer.ReadBytes(2);
                sr_code = buffer.ReadBits(2);
                frame_size_code = buffer.ReadBits(6);
                bsid = buffer.ReadBits(5);
                int bsmod = buffer.ReadBits(3);

                channel_mode = buffer.ReadBits(3);
                int cmixlev = 0;
                if (((channel_mode & 0x1) > 0) && (channel_mode != 0x1))
                {
                    cmixlev = buffer.ReadBits(2);
                }
                int surmixlev = 0;
                if ((channel_mode & 0x4) > 0)
                {
                    surmixlev = buffer.ReadBits(2);
                }
                int dsurmod = 0;
                if (channel_mode == 0x2)
                {
                    dsurmod = buffer.ReadBits(2);
                    if (dsurmod == 0x2)
                    {
                        stream.AudioMode = TSAudioMode.Surround;
                    }
                }
                lfe_on = buffer.ReadBits(1);
                dial_norm = buffer.ReadBits(5);
                int compr = 0;
                if (1 == buffer.ReadBits(1))
                {
                    compr = buffer.ReadBits(8);
                }
                int langcod = 0;
                if (1 == buffer.ReadBits(1))
                {
                    langcod = buffer.ReadBits(8);
                }
                int mixlevel = 0;
                int roomtyp = 0;
                if (1 == buffer.ReadBits(1))
                {
                    mixlevel = buffer.ReadBits(5);
                    roomtyp = buffer.ReadBits(2);
                }
                if (channel_mode == 0)
                {
                    int dialnorm2 = buffer.ReadBits(5);
                    int compr2 = 0;
                    if (1 == buffer.ReadBits(1))
                    {
                        compr2 = buffer.ReadBits(8);
                    }
                    int langcod2 = 0;
                    if (1 == buffer.ReadBits(1))
                    {
                        langcod2 = buffer.ReadBits(8);
                    }
                    int mixlevel2 = 0;
                    int roomtyp2 = 0;
                    if (1 == buffer.ReadBits(1))
                    {
                        mixlevel2 = buffer.ReadBits(5);
                        roomtyp2 = buffer.ReadBits(2);
                    }
                }
                int copyrightb = buffer.ReadBits(1);
                int origbs = buffer.ReadBits(1);
                if (bsid == 6)
                {
                    if (1 == buffer.ReadBits(1))
                    {
                        int dmixmod = buffer.ReadBits(2);
                        int ltrtcmixlev = buffer.ReadBits(3);
                        int ltrtsurmixlev = buffer.ReadBits(3);
                        int lorocmixlev = buffer.ReadBits(3);
                        int lorosurmixlev = buffer.ReadBits(3);
                    }
                    if (1 == buffer.ReadBits(1))
                    {
                        int dsurexmod = buffer.ReadBits(2);
                        int dheadphonmod = buffer.ReadBits(2);
                        if (dheadphonmod == 0x2)
                        {
                            // TODO
                        }
                        int adconvtyp = buffer.ReadBits(1);
                        int xbsi2 = buffer.ReadBits(8);
                        int encinfo = buffer.ReadBits(1);
                        if (dsurexmod == 2)
                        {
                            stream.AudioMode = TSAudioMode.Extended;
                        }
                    }
                }
            }
            else
            {
                int frame_type = buffer.ReadBits(2);
                int substreamid = buffer.ReadBits(3);
                frame_size = (buffer.ReadBits(11) + 1) << 1;

                sr_code = buffer.ReadBits(2);
                if (sr_code == 3)
                {
                    sr_code = buffer.ReadBits(2);
                }
                else
                {
                    num_blocks = buffer.ReadBits(2);
                }
                channel_mode = buffer.ReadBits(3);
                lfe_on = buffer.ReadBits(1);
            }

            switch (channel_mode)
            {
                case 0: // 1+1
                    stream.ChannelCount = 2;
                    if (stream.AudioMode == TSAudioMode.Unknown)
                    {
                        stream.AudioMode = TSAudioMode.DualMono;
                    }
                    break;
                case 1: // 1/0
                    stream.ChannelCount = 1;
                    break;
                case 2: // 2/0
                    stream.ChannelCount = 2;
                    if (stream.AudioMode == TSAudioMode.Unknown)
                    {
                        stream.AudioMode = TSAudioMode.Stereo;
                    }
                    break;
                case 3: // 3/0
                    stream.ChannelCount = 3;
                    break;
                case 4: // 2/1
                    stream.ChannelCount = 3;
                    break;
                case 5: // 3/1
                    stream.ChannelCount = 4;
                    break;
                case 6: // 2/2
                    stream.ChannelCount = 4;
                    break;
                case 7: // 3/2
                    stream.ChannelCount = 5;
                    break;
                default:
                    stream.ChannelCount = 0;
                    break;
            }

            switch (sr_code)
            {
                case 0:
                    stream.SampleRate = 48000;
                    break;
                case 1:
                    stream.SampleRate = 44100;
                    break;
                case 2:
                    stream.SampleRate = 32000;
                    break;
                default:
                    stream.SampleRate = 0;
                    break;
            }

            if (bsid <= 10)
            {
                switch (frame_size_code >> 1)
                {
                    case 18:
                        stream.BitRate = 640000;
                        break;
                    case 17:
                        stream.BitRate = 576000;
                        break;
                    case 16:
                        stream.BitRate = 512000;
                        break;
                    case 15:
                        stream.BitRate = 448000;
                        break;
                    case 14:
                        stream.BitRate = 384000;
                        break;
                    case 13:
                        stream.BitRate = 320000;
                        break;
                    case 12:
                        stream.BitRate = 256000;
                        break;
                    case 11:
                        stream.BitRate = 224000;
                        break;
                    case 10:
                        stream.BitRate = 192000;
                        break;
                    case 9:
                        stream.BitRate = 160000;
                        break;
                    case 8:
                        stream.BitRate = 128000;
                        break;
                    case 7:
                        stream.BitRate = 112000;
                        break;
                    case 6:
                        stream.BitRate = 96000;
                        break;
                    case 5:
                        stream.BitRate = 80000;
                        break;
                    case 4:
                        stream.BitRate = 64000;
                        break;
                    case 3:
                        stream.BitRate = 56000;
                        break;
                    case 2:
                        stream.BitRate = 48000;
                        break;
                    case 1:
                        stream.BitRate = 40000;
                        break;
                    case 0:
                        stream.BitRate = 32000;
                        break;
                    default:
                        stream.BitRate = 0;
                        break;
                }
            }
            else
            {
                stream.BitRate = (long)
                    (4.0 * frame_size * stream.SampleRate / (num_blocks * 256));
            }

            stream.LFE = lfe_on;
            if (stream.StreamType != TSStreamType.AC3_PLUS_AUDIO &&
                stream.StreamType != TSStreamType.AC3_PLUS_SECONDARY_AUDIO)
            {
                stream.DialNorm = dial_norm - 31;
            }
            stream.IsVBR = false;
            stream.IsInitialized = true;
        }
    }
}
