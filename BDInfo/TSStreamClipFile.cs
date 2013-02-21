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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace BDInfo
{
    public class TSStreamClipFile
    {
        public FileInfo FileInfo = null;
        public string FileType = null;
        public bool IsValid = false;
        public string Name = null;

        public Dictionary<ushort, TSStream> Streams =
            new Dictionary<ushort,TSStream>();

        public TSStreamClipFile(
            FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            Name = fileInfo.Name.ToUpper();
        }

        public void Scan()
        {
            FileStream fileStream = null;
            BinaryReader fileReader = null;

            try
            {
#if DEBUG
                Debug.WriteLine(string.Format(
                    "Scanning {0}...", Name));
#endif
                Streams.Clear();

                fileStream = File.OpenRead(FileInfo.FullName);
                fileReader = new BinaryReader(fileStream);

                byte[] data = new byte[fileStream.Length];
                fileReader.Read(data, 0, data.Length);

                byte[] fileType = new byte[8];
                Array.Copy(data, 0, fileType, 0, fileType.Length);
                
                FileType = ASCIIEncoding.ASCII.GetString(fileType);
                if (FileType != "HDMV0100" &&
                    FileType != "HDMV0200")
                {
                    throw new Exception(string.Format(
                        "Clip info file {0} has an unknown file type {1}.",
                        FileInfo.Name, FileType));
                }
#if DEBUG                
                Debug.WriteLine(string.Format(
                    "\tFileType: {0}", FileType));
#endif
                int clipIndex =
                    ((int)data[12] << 24) +
                    ((int)data[13] << 16) +
                    ((int)data[14] << 8) +
                    ((int)data[15]);

                int clipLength =
                    ((int)data[clipIndex] << 24) +
                    ((int)data[clipIndex + 1] << 16) +
                    ((int)data[clipIndex + 2] << 8) +
                    ((int)data[clipIndex + 3]);

                byte[] clipData = new byte[clipLength];
                Array.Copy(data, clipIndex + 4, clipData, 0, clipData.Length);

                int streamCount = clipData[8];
#if DEBUG
                Debug.WriteLine(string.Format(
                    "\tStreamCount: {0}", streamCount));
#endif
                int streamOffset = 10;
                for (int streamIndex = 0;
                    streamIndex < streamCount;
                    streamIndex++)
                {
                    TSStream stream = null;

                    ushort PID = (ushort)
                        ((clipData[streamOffset] << 8) + 
                          clipData[streamOffset + 1]);
                    
                    streamOffset += 2;

                    TSStreamType streamType = (TSStreamType)
                        clipData[streamOffset + 1];
                    switch (streamType)
                    {
                        case TSStreamType.MVC_VIDEO:
                            // TODO
                            break;

                        case TSStreamType.AVC_VIDEO:
                        case TSStreamType.MPEG1_VIDEO:
                        case TSStreamType.MPEG2_VIDEO:
                        case TSStreamType.VC1_VIDEO:
                        {
                            TSVideoFormat videoFormat = (TSVideoFormat)
                                (clipData[streamOffset + 2] >> 4);
                            TSFrameRate frameRate = (TSFrameRate)
                                (clipData[streamOffset + 2] & 0xF);
                            TSAspectRatio aspectRatio = (TSAspectRatio)
                                (clipData[streamOffset + 3] >> 4);

                            stream = new TSVideoStream();
                            ((TSVideoStream)stream).VideoFormat = videoFormat;
                            ((TSVideoStream)stream).AspectRatio = aspectRatio;
                            ((TSVideoStream)stream).FrameRate = frameRate;
#if DEBUG
                            Debug.WriteLine(string.Format(
                                "\t{0} {1} {2} {3} {4}",
                                PID,
                                streamType,
                                videoFormat,
                                frameRate,
                                aspectRatio));
#endif
                        }
                        break;

                        case TSStreamType.AC3_AUDIO:
                        case TSStreamType.AC3_PLUS_AUDIO:
                        case TSStreamType.AC3_PLUS_SECONDARY_AUDIO:
                        case TSStreamType.AC3_TRUE_HD_AUDIO:
                        case TSStreamType.DTS_AUDIO:
                        case TSStreamType.DTS_HD_AUDIO:
                        case TSStreamType.DTS_HD_MASTER_AUDIO:
                        case TSStreamType.DTS_HD_SECONDARY_AUDIO:
                        case TSStreamType.LPCM_AUDIO:
                        case TSStreamType.MPEG1_AUDIO:
                        case TSStreamType.MPEG2_AUDIO:
                        {
                            byte[] languageBytes = new byte[3];
                            Array.Copy(clipData, streamOffset + 3,
                                languageBytes, 0, languageBytes.Length);
                            string languageCode =
                                ASCIIEncoding.ASCII.GetString(languageBytes);

                            TSChannelLayout channelLayout = (TSChannelLayout)
                                (clipData[streamOffset + 2] >> 4);
                            TSSampleRate sampleRate = (TSSampleRate)
                                (clipData[streamOffset + 2] & 0xF);

                            stream = new TSAudioStream();
                            ((TSAudioStream)stream).LanguageCode = languageCode;
                            ((TSAudioStream)stream).ChannelLayout = channelLayout;
                            ((TSAudioStream)stream).SampleRate = TSAudioStream.ConvertSampleRate(sampleRate);
                            ((TSAudioStream)stream).LanguageCode = languageCode;
#if DEBUG
                            Debug.WriteLine(string.Format(
                                "\t{0} {1} {2} {3} {4}",
                                PID,
                                streamType,
                                languageCode,
                                channelLayout,
                                sampleRate));
#endif
                        }
                        break;

                        case TSStreamType.INTERACTIVE_GRAPHICS:
                        case TSStreamType.PRESENTATION_GRAPHICS:
                        {
                            byte[] languageBytes = new byte[3];
                            Array.Copy(clipData, streamOffset + 2,
                                languageBytes, 0, languageBytes.Length);
                            string languageCode =
                                ASCIIEncoding.ASCII.GetString(languageBytes);

                            stream = new TSGraphicsStream();
                            stream.LanguageCode = languageCode;
#if DEBUG
                            Debug.WriteLine(string.Format(
                                "\t{0} {1} {2}",
                                PID,
                                streamType,
                                languageCode));
#endif
                        }
                        break;

                        case TSStreamType.SUBTITLE:
                        {
                            byte[] languageBytes = new byte[3];
                            Array.Copy(clipData, streamOffset + 3,
                                languageBytes, 0, languageBytes.Length);
                            string languageCode =
                                ASCIIEncoding.ASCII.GetString(languageBytes);
#if DEBUG
                            Debug.WriteLine(string.Format(
                                "\t{0} {1} {2}",
                                PID,
                                streamType,
                                languageCode));
#endif
                            stream = new TSTextStream();
                            stream.LanguageCode = languageCode;
                        }
                        break;
                    }

                    if (stream != null)
                    {
                        stream.PID = PID;
                        stream.StreamType = streamType;
                        Streams.Add(PID, stream);
                    }

                    streamOffset += clipData[streamOffset] + 1;
                }                
                IsValid = true;
            }
            finally
            {
                if (fileReader != null) fileReader.Close();
                if (fileStream != null) fileStream.Close();
            }
        }
    }
}
