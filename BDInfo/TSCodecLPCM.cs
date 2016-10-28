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


namespace BDInfo
{
    public abstract class TSCodecLPCM
    {
        public static void Scan(
            TSAudioStream stream,
            TSStreamBuffer buffer,
            ref string tag)
        {
            if (stream.IsInitialized) return;

            byte[] header = buffer.ReadBytes(4);
            int flags = (header[2] << 8) + header[3];

            switch ((flags & 0xF000) >> 12)
            {
                case 1: // 1/0/0
                    stream.ChannelCount = 1;
                    stream.LFE = 0;
                    break;
                case 3: // 2/0/0
                    stream.ChannelCount = 2;
                    stream.LFE = 0;
                    break;
                case 4: // 3/0/0
                    stream.ChannelCount = 3;
                    stream.LFE = 0;
                    break;
                case 5: // 2/1/0
                    stream.ChannelCount = 3;
                    stream.LFE = 0;
                    break;
                case 6: // 3/1/0
                    stream.ChannelCount = 4;
                    stream.LFE = 0;
                    break;
                case 7: // 2/2/0
                    stream.ChannelCount = 4;
                    stream.LFE = 0;
                    break;
                case 8: // 3/2/0
                    stream.ChannelCount = 5;
                    stream.LFE = 0;
                    break;
                case 9: // 3/2/1
                    stream.ChannelCount = 5;
                    stream.LFE = 1;
                    break;
                case 10: // 3/4/0
                    stream.ChannelCount = 7;
                    stream.LFE = 0;
                    break;
                case 11: // 3/4/1
                    stream.ChannelCount = 7;
                    stream.LFE = 1;
                    break;
                default:
                    stream.ChannelCount = 0;
                    stream.LFE = 0;
                    break;
            }

            switch ((flags & 0xC0) >> 6)
            {
                case 1:
                    stream.BitDepth = 16;
                    break;
                case 2:
                    stream.BitDepth = 20;
                    break;
                case 3:
                    stream.BitDepth = 24;
                    break;
                default:
                    stream.BitDepth = 0;
                    break;
            }

            switch ((flags & 0xF00) >> 8)
            {
                case 1:
                    stream.SampleRate = 48000;
                    break;
                case 4:
                    stream.SampleRate = 96000;
                    break;
                case 5:
                    stream.SampleRate = 192000;
                    break;
                default:
                    stream.SampleRate = 0;
                    break;
            }

            stream.BitRate = (uint)
                (stream.SampleRate * stream.BitDepth *
                 (stream.ChannelCount + stream.LFE));

            stream.IsVBR = false;
            stream.IsInitialized = true;
        }
    }
}
