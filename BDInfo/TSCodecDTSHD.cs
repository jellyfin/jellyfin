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
    public abstract class TSCodecDTSHD
    {
        private static int[] SampleRates = new int[] 
        { 0x1F40, 0x3E80, 0x7D00, 0x0FA00, 0x1F400, 0x5622, 0x0AC44, 0x15888, 0x2B110, 0x56220, 0x2EE0, 0x5DC0, 0x0BB80, 0x17700, 0x2EE00, 0x5DC00 };
        
        public static void Scan(
            TSAudioStream stream,
            TSStreamBuffer buffer,
            long bitrate,
            ref string tag)
        {
            if (stream.IsInitialized &&
                (stream.StreamType == TSStreamType.DTS_HD_SECONDARY_AUDIO ||
                (stream.CoreStream != null &&
                 stream.CoreStream.IsInitialized))) return;

            bool syncFound = false;
            uint sync = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                sync = (sync << 8) + buffer.ReadByte();
                if (sync == 0x64582025)
                {
                    syncFound = true;
                    break;
                }
            }

            if (!syncFound)
            {
                tag = "CORE";
                if (stream.CoreStream == null)
                {
                    stream.CoreStream = new TSAudioStream();
                    stream.CoreStream.StreamType = TSStreamType.DTS_AUDIO;
                }
                if (!stream.CoreStream.IsInitialized)
                {
                    buffer.BeginRead();
                    TSCodecDTS.Scan(stream.CoreStream, buffer, bitrate, ref tag);
                }
                return;
            }

            tag = "HD";
            int temp1 = buffer.ReadBits(8);
            int nuSubStreamIndex = buffer.ReadBits(2);
            int nuExtSSHeaderSize = 0;
            int nuExtSSFSize = 0;
            int bBlownUpHeader = buffer.ReadBits(1);
            if (1 == bBlownUpHeader)
            {
                nuExtSSHeaderSize = buffer.ReadBits(12) + 1;
                nuExtSSFSize = buffer.ReadBits(20) + 1;
            }
            else
            {
                nuExtSSHeaderSize = buffer.ReadBits(8) + 1;
                nuExtSSFSize = buffer.ReadBits(16) + 1;
            }
            int nuNumAudioPresent = 1;
            int nuNumAssets = 1;
            int bStaticFieldsPresent = buffer.ReadBits(1);
            if (1 == bStaticFieldsPresent)
            {
                int nuRefClockCode = buffer.ReadBits(2);
                int nuExSSFrameDurationCode = buffer.ReadBits(3) + 1;
                long nuTimeStamp = 0;
                if (1 == buffer.ReadBits(1))
                {
                    nuTimeStamp = (buffer.ReadBits(18) << 18) + buffer.ReadBits(18);
                }
                nuNumAudioPresent = buffer.ReadBits(3) + 1;
                nuNumAssets = buffer.ReadBits(3) + 1;
                int[] nuActiveExSSMask = new int[nuNumAudioPresent];
                for (int i = 0; i < nuNumAudioPresent; i++)
                {
                    nuActiveExSSMask[i] = buffer.ReadBits(nuSubStreamIndex + 1); //?
                }
                for (int i = 0; i < nuNumAudioPresent; i++)
                {
                    for (int j = 0; j < nuSubStreamIndex + 1; j++)
                    {
                        if (((j + 1) % 2) == 1)
                        {
                            int mask = buffer.ReadBits(8);
                        }
                    }
                }
                if (1 == buffer.ReadBits(1))
                {
                    int nuMixMetadataAdjLevel = buffer.ReadBits(2);
                    int nuBits4MixOutMask = buffer.ReadBits(2) * 4 + 4;
                    int nuNumMixOutConfigs = buffer.ReadBits(2) + 1;
                    int[] nuMixOutChMask = new int[nuNumMixOutConfigs];
                    for (int i = 0; i < nuNumMixOutConfigs; i++)
                    {
                        nuMixOutChMask[i] = buffer.ReadBits(nuBits4MixOutMask);
                    }
                }
            }
            int[] AssetSizes = new int[nuNumAssets];
            for (int i = 0; i < nuNumAssets; i++)
            {
                if (1 == bBlownUpHeader)
                {
                    AssetSizes[i] = buffer.ReadBits(20) + 1;
                }
                else
                {
                    AssetSizes[i] = buffer.ReadBits(16) + 1;
                }                
            }
            for (int i = 0; i < nuNumAssets; i++)
            {
                long bufferPosition = buffer.Position;
                int nuAssetDescriptorFSIZE = buffer.ReadBits(9) + 1;
                int DescriptorDataForAssetIndex = buffer.ReadBits(3);
                if (1 == bStaticFieldsPresent)
                {
                    int AssetTypeDescrPresent = buffer.ReadBits(1);
                    if (1 == AssetTypeDescrPresent)
                    {
                        int AssetTypeDescriptor = buffer.ReadBits(4);
                    }
                    int LanguageDescrPresent = buffer.ReadBits(1);
                    if (1 == LanguageDescrPresent)
                    {
                        int LanguageDescriptor = buffer.ReadBits(24);
                    }
                    int bInfoTextPresent = buffer.ReadBits(1);
                    if (1 == bInfoTextPresent)
                    {
                        int nuInfoTextByteSize = buffer.ReadBits(10) + 1;
                        int[] InfoText = new int[nuInfoTextByteSize];
                        for (int j = 0; j < nuInfoTextByteSize; j++)
                        {
                            InfoText[j] = buffer.ReadBits(8);
                        }
                    }
                    int nuBitResolution = buffer.ReadBits(5) + 1;
                    int nuMaxSampleRate = buffer.ReadBits(4);
                    int nuTotalNumChs = buffer.ReadBits(8) + 1;
                    int bOne2OneMapChannels2Speakers = buffer.ReadBits(1);
                    int nuSpkrActivityMask = 0;
                    if (1 == bOne2OneMapChannels2Speakers)
                    {
                        int bEmbeddedStereoFlag = 0;
                        if (nuTotalNumChs > 2)
                        {
                            bEmbeddedStereoFlag = buffer.ReadBits(1);
                        }
                        int bEmbeddedSixChFlag = 0;
                        if (nuTotalNumChs > 6)
                        {
                            bEmbeddedSixChFlag = buffer.ReadBits(1);
                        }
                        int bSpkrMaskEnabled = buffer.ReadBits(1);
                        int nuNumBits4SAMask = 0;
                        if (1 == bSpkrMaskEnabled)
                        {
                            nuNumBits4SAMask = buffer.ReadBits(2);
                            nuNumBits4SAMask = nuNumBits4SAMask * 4 + 4;
                            nuSpkrActivityMask = buffer.ReadBits(nuNumBits4SAMask);
                        }
                        // TODO...
                    }
                    stream.SampleRate = SampleRates[nuMaxSampleRate];
                    stream.BitDepth = nuBitResolution;
                    
                    stream.LFE = 0;
                    if ((nuSpkrActivityMask & 0x8) == 0x8)
                    {
                        ++stream.LFE;
                    }
                    if ((nuSpkrActivityMask & 0x1000) == 0x1000)
                    {
                        ++stream.LFE;
                    }
                    stream.ChannelCount = nuTotalNumChs - stream.LFE;
                }
                if (nuNumAssets > 1)
                {
                    // TODO...
                    break;
                }
            }

            // TODO
            if (stream.CoreStream != null)
            {
                TSAudioStream coreStream = (TSAudioStream)stream.CoreStream;
                if (coreStream.AudioMode == TSAudioMode.Extended &&
                    stream.ChannelCount == 5)
                {
                    stream.AudioMode = TSAudioMode.Extended;
                }
                /*
                if (coreStream.DialNorm != 0)
                {
                    stream.DialNorm = coreStream.DialNorm;
                }
                */
            }

            if (stream.StreamType == TSStreamType.DTS_HD_MASTER_AUDIO)
            {
                stream.IsVBR = true;
                stream.IsInitialized = true;
            }
            else if (bitrate > 0)
            {
                stream.IsVBR = false;
                stream.BitRate = bitrate;
                if (stream.CoreStream != null)
                {
                    stream.BitRate += stream.CoreStream.BitRate;
                    stream.IsInitialized = true;
                }
                stream.IsInitialized = (stream.BitRate > 0 ? true : false);
            }            
        }
    }
}
