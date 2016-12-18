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
    public abstract class TSCodecDTS
    {
        private static int[] dca_sample_rates =
        {
            0, 8000, 16000, 32000, 0, 0, 11025, 22050, 44100, 0, 0,
            12000, 24000, 48000, 96000, 192000
        };

        private static int[] dca_bit_rates =
        {
            32000, 56000, 64000, 96000, 112000, 128000,
            192000, 224000, 256000, 320000, 384000,
            448000, 512000, 576000, 640000, 768000,
            896000, 1024000, 1152000, 1280000, 1344000,
            1408000, 1411200, 1472000, 1509000, 1920000,
            2048000, 3072000, 3840000, 1/*open*/, 2/*variable*/, 3/*lossless*/
        };

        private static int[] dca_channels =
        {
            1, 2, 2, 2, 2, 3, 3, 4, 4, 5, 6, 6, 6, 7, 8, 8
        };

        private static int[] dca_bits_per_sample =
        {
            16, 16, 20, 20, 0, 24, 24
        };

        public static void Scan(
            TSAudioStream stream,
            TSStreamBuffer buffer,
            long bitrate,
            ref string tag)
        {
            if (stream.IsInitialized) return;

            bool syncFound = false;
            uint sync = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                sync = (sync << 8) + buffer.ReadByte();
                if (sync == 0x7FFE8001)
                {
                    syncFound = true;
                    break;
                }
            }
            if (!syncFound) return;

            int frame_type = buffer.ReadBits(1);
            int samples_deficit = buffer.ReadBits(5);
            int crc_present = buffer.ReadBits(1);
            int sample_blocks = buffer.ReadBits(7);
            int frame_size = buffer.ReadBits(14);
            if (frame_size < 95)
            {
                return;
            }
            int amode = buffer.ReadBits(6);
            int sample_rate = buffer.ReadBits(4);
            if (sample_rate < 0 || sample_rate >= dca_sample_rates.Length)
            {
                return;
            }
            int bit_rate = buffer.ReadBits(5);
            if (bit_rate < 0 || bit_rate >= dca_bit_rates.Length)
            {
                return;
            }
            int downmix = buffer.ReadBits(1);
            int dynrange = buffer.ReadBits(1);
            int timestamp = buffer.ReadBits(1);
            int aux_data = buffer.ReadBits(1);
            int hdcd = buffer.ReadBits(1);
            int ext_descr = buffer.ReadBits(3);
            int ext_coding = buffer.ReadBits(1);
            int aspf = buffer.ReadBits(1);
            int lfe = buffer.ReadBits(2);
            int predictor_history = buffer.ReadBits(1);
            if (crc_present == 1)
            {
                int crc = buffer.ReadBits(16);
            }
            int multirate_inter = buffer.ReadBits(1);
            int version = buffer.ReadBits(4);
            int copy_history = buffer.ReadBits(2);
            int source_pcm_res = buffer.ReadBits(3);
            int front_sum = buffer.ReadBits(1);
            int surround_sum = buffer.ReadBits(1);
            int dialog_norm = buffer.ReadBits(4);
            if (source_pcm_res < 0 || source_pcm_res >= dca_bits_per_sample.Length)
            {
                return;
            }
            int subframes = buffer.ReadBits(4);
            int total_channels = buffer.ReadBits(3) + 1 + ext_coding;

            stream.SampleRate = dca_sample_rates[sample_rate];
            stream.ChannelCount = total_channels;
            stream.LFE = (lfe > 0 ? 1 : 0);
            stream.BitDepth = dca_bits_per_sample[source_pcm_res];
            stream.DialNorm = -dialog_norm;
            if ((source_pcm_res & 0x1) == 0x1)
            {
                stream.AudioMode = TSAudioMode.Extended;
            }

            stream.BitRate = (uint)dca_bit_rates[bit_rate];
            switch (stream.BitRate)
            {
                case 1:
                    if (bitrate > 0)
                    {
                        stream.BitRate = bitrate;
                        stream.IsVBR = false;
                        stream.IsInitialized = true;
                    }
                    else
                    {
                        stream.BitRate = 0;
                    }
                    break;

                case 2:
                case 3:
                    stream.IsVBR = true;
                    stream.IsInitialized = true;
                    break;
                
                default:
                    stream.IsVBR = false;
                    stream.IsInitialized = true;
                    break;
            }
        }
    }
}
