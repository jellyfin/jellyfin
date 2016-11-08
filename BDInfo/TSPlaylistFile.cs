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
using System.IO;
using System.Text;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Text;

namespace BDInfo
{
    public class TSPlaylistFile
    {
        private readonly IFileSystem _fileSystem;
        private readonly ITextEncoding _textEncoding;
        private FileSystemMetadata FileInfo = null;
        public string FileType = null;
        public bool IsInitialized = false;
        public string Name = null;
        public BDROM BDROM = null;
        public bool HasHiddenTracks = false;
        public bool HasLoops = false;
        public bool IsCustom = false;

        public List<double> Chapters = new List<double>();

        public Dictionary<ushort, TSStream> Streams = 
            new Dictionary<ushort, TSStream>();
        public Dictionary<ushort, TSStream> PlaylistStreams =
            new Dictionary<ushort, TSStream>();
        public List<TSStreamClip> StreamClips =
            new List<TSStreamClip>();
        public List<Dictionary<ushort, TSStream>> AngleStreams =
            new List<Dictionary<ushort, TSStream>>();
        public List<Dictionary<double, TSStreamClip>> AngleClips = 
            new List<Dictionary<double, TSStreamClip>>();
        public int AngleCount = 0;

        public List<TSStream> SortedStreams = 
            new List<TSStream>();
        public List<TSVideoStream> VideoStreams = 
            new List<TSVideoStream>();
        public List<TSAudioStream> AudioStreams = 
            new List<TSAudioStream>();
        public List<TSTextStream> TextStreams = 
            new List<TSTextStream>();
        public List<TSGraphicsStream> GraphicsStreams = 
            new List<TSGraphicsStream>();

        public TSPlaylistFile(
            BDROM bdrom,
            FileSystemMetadata fileInfo, IFileSystem fileSystem, ITextEncoding textEncoding)
        {
            BDROM = bdrom;
            FileInfo = fileInfo;
            _fileSystem = fileSystem;
            _textEncoding = textEncoding;
            Name = fileInfo.Name.ToUpper();
        }

        public TSPlaylistFile(
            BDROM bdrom,
            string name,
            List<TSStreamClip> clips, IFileSystem fileSystem, ITextEncoding textEncoding)
        {
            BDROM = bdrom;
            Name = name;
            _fileSystem = fileSystem;
            _textEncoding = textEncoding;
            IsCustom = true;
            foreach (TSStreamClip clip in clips)
            {
                TSStreamClip newClip = new TSStreamClip(
                    clip.StreamFile, clip.StreamClipFile);

                newClip.Name = clip.Name;
                newClip.TimeIn = clip.TimeIn;
                newClip.TimeOut = clip.TimeOut;
                newClip.Length = newClip.TimeOut - newClip.TimeIn;
                newClip.RelativeTimeIn = TotalLength;
                newClip.RelativeTimeOut = newClip.RelativeTimeIn + newClip.Length;
                newClip.AngleIndex = clip.AngleIndex;
                newClip.Chapters.Add(clip.TimeIn);
                StreamClips.Add(newClip);

                if (newClip.AngleIndex > AngleCount)
                {
                    AngleCount = newClip.AngleIndex;
                }
                if (newClip.AngleIndex == 0)
                {
                    Chapters.Add(newClip.RelativeTimeIn);
                }
            }
            LoadStreamClips();
            IsInitialized = true;
        }

        public override string ToString()
        {
            return Name;
        }

        public ulong InterleavedFileSize
        {
            get
            {
                ulong size = 0;
                foreach (TSStreamClip clip in StreamClips)
                {
                    size += clip.InterleavedFileSize;
                }
                return size;
            }
        }
        public ulong FileSize
        {
            get
            {
                ulong size = 0;
                foreach (TSStreamClip clip in StreamClips)
                {
                    size += clip.FileSize;
                }
                return size;
            }
        }
        public double TotalLength
        {
            get
            {
                double length = 0;
                foreach (TSStreamClip clip in StreamClips)
                {
                    if (clip.AngleIndex == 0)
                    {
                        length += clip.Length;
                    }
                }
                return length;
            }
        }

        public double TotalAngleLength
        {
            get
            {
                double length = 0;
                foreach (TSStreamClip clip in StreamClips)
                {
                    length += clip.Length;
                }
                return length;
            }
        }

        public ulong TotalSize
        {
            get
            {
                ulong size = 0;
                foreach (TSStreamClip clip in StreamClips)
                {
                    if (clip.AngleIndex == 0)
                    {
                        size += clip.PacketSize;
                    }
                }
                return size;
            }
        }

        public ulong TotalAngleSize
        {
            get
            {
                ulong size = 0;
                foreach (TSStreamClip clip in StreamClips)
                {
                    size += clip.PacketSize;
                }
                return size;
            }
        }

        public ulong TotalBitRate
        {
            get
            {
                if (TotalLength > 0)
                {
                    return (ulong)Math.Round(((TotalSize * 8.0) / TotalLength));
                }
                return 0;
            }
        }

        public ulong TotalAngleBitRate
        {
            get
            {
                if (TotalAngleLength > 0)
                {
                    return (ulong)Math.Round(((TotalAngleSize * 8.0) / TotalAngleLength));
                }
                return 0;
            }
        }

        public void Scan(
            Dictionary<string, TSStreamFile> streamFiles,
            Dictionary<string, TSStreamClipFile> streamClipFiles)
        {
            Stream fileStream = null;
            BinaryReader fileReader = null;

            try
            {
                Streams.Clear();
                StreamClips.Clear();

                fileStream = _fileSystem.OpenRead(FileInfo.FullName);
                fileReader = new BinaryReader(fileStream);

                byte[] data = new byte[fileStream.Length];
                int dataLength = fileReader.Read(data, 0, data.Length);

                int pos = 0;

                FileType = ReadString(data, 8, ref pos);
                if (FileType != "MPLS0100" && FileType != "MPLS0200")
                {
                    throw new Exception(string.Format(
                        "Playlist {0} has an unknown file type {1}.",
                        FileInfo.Name, FileType));
                }

                int playlistOffset = ReadInt32(data, ref pos);
                int chaptersOffset = ReadInt32(data, ref pos);
                int extensionsOffset = ReadInt32(data, ref pos);

                pos = playlistOffset;

                int playlistLength = ReadInt32(data, ref pos);
                int playlistReserved = ReadInt16(data, ref pos);
                int itemCount = ReadInt16(data, ref pos);
                int subitemCount = ReadInt16(data, ref pos);

                List<TSStreamClip> chapterClips = new List<TSStreamClip>();
                for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
                {
                    int itemStart = pos;
                    int itemLength = ReadInt16(data, ref pos);
                    string itemName = ReadString(data, 5, ref pos);
                    string itemType = ReadString(data, 4, ref pos);

                    TSStreamFile streamFile = null;
                    string streamFileName = string.Format(
                        "{0}.M2TS", itemName);
                    if (streamFiles.ContainsKey(streamFileName))
                    {
                        streamFile = streamFiles[streamFileName];
                    }
                    if (streamFile == null)
                    {
                        // Error condition
                    }

                    TSStreamClipFile streamClipFile = null;
                    string streamClipFileName = string.Format(
                        "{0}.CLPI", itemName);
                    if (streamClipFiles.ContainsKey(streamClipFileName))
                    {
                        streamClipFile = streamClipFiles[streamClipFileName];
                    }
                    if (streamClipFile == null)
                    {
                        throw new Exception(string.Format(
                            "Playlist {0} referenced missing file {1}.",
                            FileInfo.Name, streamFileName));
                    }

                    pos += 1;
                    int multiangle = (data[pos] >> 4) & 0x01;
                    int condition = data[pos] & 0x0F;
                    pos += 2;

                    int inTime = ReadInt32(data, ref pos);
                    if (inTime < 0) inTime &= 0x7FFFFFFF;
                    double timeIn = (double)inTime / 45000;

                    int outTime = ReadInt32(data, ref pos);
                    if (outTime < 0) outTime &= 0x7FFFFFFF;
                    double timeOut = (double)outTime / 45000;

                    TSStreamClip streamClip = new TSStreamClip(
                        streamFile, streamClipFile);

                    streamClip.Name = streamFileName; //TODO
                    streamClip.TimeIn = timeIn;
                    streamClip.TimeOut = timeOut;
                    streamClip.Length = streamClip.TimeOut - streamClip.TimeIn;
                    streamClip.RelativeTimeIn = TotalLength;
                    streamClip.RelativeTimeOut = streamClip.RelativeTimeIn + streamClip.Length;
                    StreamClips.Add(streamClip);
                    chapterClips.Add(streamClip);

                    pos += 12;
                    if (multiangle > 0)
                    {
                        int angles = data[pos];
                        pos += 2;
                        for (int angle = 0; angle < angles - 1; angle++)
                        {
                            string angleName = ReadString(data, 5, ref pos);
                            string angleType = ReadString(data, 4, ref pos);
                            pos += 1;

                            TSStreamFile angleFile = null;
                            string angleFileName = string.Format(
                                "{0}.M2TS", angleName);
                            if (streamFiles.ContainsKey(angleFileName))
                            {
                                angleFile = streamFiles[angleFileName];
                            }
                            if (angleFile == null)
                            {
                                throw new Exception(string.Format(
                                    "Playlist {0} referenced missing angle file {1}.",
                                    FileInfo.Name, angleFileName));
                            }

                            TSStreamClipFile angleClipFile = null;
                            string angleClipFileName = string.Format(
                                "{0}.CLPI", angleName);
                            if (streamClipFiles.ContainsKey(angleClipFileName))
                            {
                                angleClipFile = streamClipFiles[angleClipFileName];
                            }
                            if (angleClipFile == null)
                            {
                                throw new Exception(string.Format(
                                    "Playlist {0} referenced missing angle file {1}.",
                                    FileInfo.Name, angleClipFileName));
                            }

                            TSStreamClip angleClip =
                                new TSStreamClip(angleFile, angleClipFile);
                            angleClip.AngleIndex = angle + 1;
                            angleClip.TimeIn = streamClip.TimeIn;
                            angleClip.TimeOut = streamClip.TimeOut;
                            angleClip.RelativeTimeIn = streamClip.RelativeTimeIn;
                            angleClip.RelativeTimeOut = streamClip.RelativeTimeOut;
                            angleClip.Length = streamClip.Length;
                            StreamClips.Add(angleClip);
                        }
                        if (angles - 1 > AngleCount) AngleCount = angles - 1;
                    }

                    int streamInfoLength = ReadInt16(data, ref pos);
                    pos += 2;
                    int streamCountVideo = data[pos++];
                    int streamCountAudio = data[pos++];
                    int streamCountPG = data[pos++];
                    int streamCountIG = data[pos++];
                    int streamCountSecondaryAudio = data[pos++];
                    int streamCountSecondaryVideo = data[pos++];
                    int streamCountPIP = data[pos++];
                    pos += 5;

#if DEBUG
                    Debug.WriteLine(string.Format(
                        "{0} : {1} -> V:{2} A:{3} PG:{4} IG:{5} 2A:{6} 2V:{7} PIP:{8}", 
                        Name, streamFileName, streamCountVideo, streamCountAudio, streamCountPG, streamCountIG, 
                        streamCountSecondaryAudio, streamCountSecondaryVideo, streamCountPIP));
#endif

                    for (int i = 0; i < streamCountVideo; i++)
                    {
                        TSStream stream = CreatePlaylistStream(data, ref pos);
                        if (stream != null) PlaylistStreams[stream.PID] = stream;
                    }
                    for (int i = 0; i < streamCountAudio; i++)
                    {
                        TSStream stream = CreatePlaylistStream(data, ref pos);
                        if (stream != null) PlaylistStreams[stream.PID] = stream;
                    }
                    for (int i = 0; i < streamCountPG; i++)
                    {
                        TSStream stream = CreatePlaylistStream(data, ref pos);
                        if (stream != null) PlaylistStreams[stream.PID] = stream;
                    }
                    for (int i = 0; i < streamCountIG; i++)
                    {
                        TSStream stream = CreatePlaylistStream(data, ref pos);
                        if (stream != null) PlaylistStreams[stream.PID] = stream;
                    }
                    for (int i = 0; i < streamCountSecondaryAudio; i++)
                    {
                        TSStream stream = CreatePlaylistStream(data, ref pos);
                        if (stream != null) PlaylistStreams[stream.PID] = stream;
                        pos += 2;
                    }
                    for (int i = 0; i < streamCountSecondaryVideo; i++)
                    {
                        TSStream stream = CreatePlaylistStream(data, ref pos);
                        if (stream != null) PlaylistStreams[stream.PID] = stream;
                        pos += 6;
                    }
                    /*
                     * TODO
                     * 
                    for (int i = 0; i < streamCountPIP; i++)
                    {
                        TSStream stream = CreatePlaylistStream(data, ref pos);
                        if (stream != null) PlaylistStreams[stream.PID] = stream;
                    }
                    */

                    pos += itemLength - (pos - itemStart) + 2;
                }

                pos = chaptersOffset + 4;

                int chapterCount = ReadInt16(data, ref pos);

                for (int chapterIndex = 0;
                    chapterIndex < chapterCount;
                    chapterIndex++)
                {
                    int chapterType = data[pos+1];

                    if (chapterType == 1)
                    {
                        int streamFileIndex =
                            ((int)data[pos + 2] << 8) + data[pos + 3];

                        long chapterTime =
                            ((long)data[pos + 4] << 24) +
                            ((long)data[pos + 5] << 16) +
                            ((long)data[pos + 6] << 8) +
                            ((long)data[pos + 7]);

                        TSStreamClip streamClip = chapterClips[streamFileIndex];

                        double chapterSeconds = (double)chapterTime / 45000;

                        double relativeSeconds =
                            chapterSeconds -
                            streamClip.TimeIn +
                            streamClip.RelativeTimeIn;

                        // TODO: Ignore short last chapter?
                        if (TotalLength - relativeSeconds > 1.0)
                        {
                            streamClip.Chapters.Add(chapterSeconds);
                            this.Chapters.Add(relativeSeconds);
                        }
                    }
                    else
                    {
                        // TODO: Handle other chapter types?
                    }
                    pos += 14;
                }
            }
            finally
            {
                if (fileReader != null)
                {
                    fileReader.Dispose();
                }
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
            }
        }

        public void Initialize()
        {
            LoadStreamClips();

            Dictionary<string, List<double>> clipTimes = new Dictionary<string, List<double>>();
            foreach (TSStreamClip clip in StreamClips)
            {
                if (clip.AngleIndex == 0)
                {
                    if (clipTimes.ContainsKey(clip.Name))
                    {
                        if (clipTimes[clip.Name].Contains(clip.TimeIn))
                        {
                            HasLoops = true;
                            break;
                        }
                        else
                        {
                            clipTimes[clip.Name].Add(clip.TimeIn);
                        }
                    }
                    else
                    {
                        clipTimes[clip.Name] = new List<double> { clip.TimeIn };
                    }
                }
            }
            ClearBitrates();
            IsInitialized = true;
        }

        protected TSStream CreatePlaylistStream(byte[] data, ref int pos)
        {
            TSStream stream = null;

            int start = pos;

            int headerLength = data[pos++];
            int headerPos = pos;
            int headerType = data[pos++];

            int pid = 0;
            int subpathid = 0;
            int subclipid = 0;

            switch (headerType)
            {
                case 1:
                    pid = ReadInt16(data, ref pos);
                    break;
                case 2:
                    subpathid = data[pos++];
                    subclipid = data[pos++];
                    pid = ReadInt16(data, ref pos);
                    break;
                case 3:
                    subpathid = data[pos++];
                    pid = ReadInt16(data, ref pos);
                    break;
                case 4:
                    subpathid = data[pos++];
                    subclipid = data[pos++];
                    pid = ReadInt16(data, ref pos);
                    break;
                default:
                    break;
            }

            pos = headerPos + headerLength;

            int streamLength = data[pos++];
            int streamPos = pos;

            TSStreamType streamType = (TSStreamType)data[pos++];
            switch (streamType)
            {
                case TSStreamType.MVC_VIDEO:
                    // TODO
                    break;

                case TSStreamType.AVC_VIDEO:
                case TSStreamType.MPEG1_VIDEO:
                case TSStreamType.MPEG2_VIDEO:
                case TSStreamType.VC1_VIDEO:

                    TSVideoFormat videoFormat = (TSVideoFormat)
                        (data[pos] >> 4);
                    TSFrameRate frameRate = (TSFrameRate)
                        (data[pos] & 0xF);
                    TSAspectRatio aspectRatio = (TSAspectRatio)
                        (data[pos + 1] >> 4);

                    stream = new TSVideoStream();
                    ((TSVideoStream)stream).VideoFormat = videoFormat;
                    ((TSVideoStream)stream).AspectRatio = aspectRatio;
                    ((TSVideoStream)stream).FrameRate = frameRate;

#if DEBUG
                            Debug.WriteLine(string.Format(
                                "\t{0} {1} {2} {3} {4}",
                                pid,
                                streamType,
                                videoFormat,
                                frameRate,
                                aspectRatio));
#endif

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

                    int audioFormat = ReadByte(data, ref pos);

                    TSChannelLayout channelLayout = (TSChannelLayout)
                        (audioFormat >> 4);
                    TSSampleRate sampleRate = (TSSampleRate)
                        (audioFormat & 0xF);

                    string audioLanguage = ReadString(data, 3, ref pos);

                    stream = new TSAudioStream();
                    ((TSAudioStream)stream).ChannelLayout = channelLayout;
                    ((TSAudioStream)stream).SampleRate = TSAudioStream.ConvertSampleRate(sampleRate);
                    ((TSAudioStream)stream).LanguageCode = audioLanguage;

#if DEBUG
                    Debug.WriteLine(string.Format(
                        "\t{0} {1} {2} {3} {4}",
                        pid,
                        streamType,
                        audioLanguage,
                        channelLayout,
                        sampleRate));
#endif

                    break;

                case TSStreamType.INTERACTIVE_GRAPHICS:
                case TSStreamType.PRESENTATION_GRAPHICS:

                    string graphicsLanguage = ReadString(data, 3, ref pos);

                    stream = new TSGraphicsStream();
                    ((TSGraphicsStream)stream).LanguageCode = graphicsLanguage;

                    if (data[pos] != 0)
                    {
                    }

#if DEBUG
                    Debug.WriteLine(string.Format(
                        "\t{0} {1} {2}",
                        pid,
                        streamType,
                        graphicsLanguage));
#endif

                    break;

                case TSStreamType.SUBTITLE:

                    int code = ReadByte(data, ref pos); // TODO
                    string textLanguage = ReadString(data, 3, ref pos);

                    stream = new TSTextStream();
                    ((TSTextStream)stream).LanguageCode = textLanguage;

#if DEBUG
                    Debug.WriteLine(string.Format(
                        "\t{0} {1} {2}",
                        pid,
                        streamType,
                        textLanguage));
#endif

                    break;

                default:
                    break;
            }

            pos = streamPos + streamLength;

            if (stream != null)
            {
                stream.PID = (ushort)pid;
                stream.StreamType = streamType;
            }

            return stream;
        }

        private void LoadStreamClips()
        {
            AngleClips.Clear();
            if (AngleCount > 0)
            {
                for (int angleIndex = 0; angleIndex < AngleCount; angleIndex++)
                {
                    AngleClips.Add(new Dictionary<double, TSStreamClip>());
                }
            }

            TSStreamClip referenceClip = null;
            if (StreamClips.Count > 0)
            {
                referenceClip = StreamClips[0];
            }
            foreach (TSStreamClip clip in StreamClips)
            {
                if (clip.StreamClipFile.Streams.Count > referenceClip.StreamClipFile.Streams.Count)
                {
                    referenceClip = clip;
                }
                else if (clip.Length > referenceClip.Length)
                {
                    referenceClip = clip;
                }
                if (AngleCount > 0)
                {
                    if (clip.AngleIndex == 0)
                    {
                        for (int angleIndex = 0; angleIndex < AngleCount; angleIndex++)
                        {
                            AngleClips[angleIndex][clip.RelativeTimeIn] = clip;
                        }
                    }
                    else
                    {
                        AngleClips[clip.AngleIndex - 1][clip.RelativeTimeIn] = clip;
                    }
                }
            }

            foreach (TSStream clipStream
                in referenceClip.StreamClipFile.Streams.Values)
            {
                if (!Streams.ContainsKey(clipStream.PID))
                {
                    TSStream stream = clipStream.Clone();
                    Streams[clipStream.PID] = stream;

                    if (!IsCustom && !PlaylistStreams.ContainsKey(stream.PID))
                    {
                        stream.IsHidden = true;
                        HasHiddenTracks = true;
                    }

                    if (stream.IsVideoStream)
                    {
                        VideoStreams.Add((TSVideoStream)stream);
                    }
                    else if (stream.IsAudioStream)
                    {
                        AudioStreams.Add((TSAudioStream)stream);
                    }
                    else if (stream.IsGraphicsStream)
                    {
                        GraphicsStreams.Add((TSGraphicsStream)stream);
                    }
                    else if (stream.IsTextStream)
                    {
                        TextStreams.Add((TSTextStream)stream);
                    }
                }
            }

            if (referenceClip.StreamFile != null)
            {
                // TODO: Better way to add this in?
                if (BDInfoSettings.EnableSSIF &&
                    referenceClip.StreamFile.InterleavedFile != null &&
                    referenceClip.StreamFile.Streams.ContainsKey(4114) &&
                    !Streams.ContainsKey(4114))
                {
                    TSStream stream = referenceClip.StreamFile.Streams[4114].Clone();
                    Streams[4114] = stream;
                    if (stream.IsVideoStream)
                    {
                        VideoStreams.Add((TSVideoStream)stream);
                    }
                }

                foreach (TSStream clipStream
                    in referenceClip.StreamFile.Streams.Values)
                {
                    if (Streams.ContainsKey(clipStream.PID))
                    {
                        TSStream stream = Streams[clipStream.PID];

                        if (stream.StreamType != clipStream.StreamType) continue;

                        if (clipStream.BitRate > stream.BitRate)
                        {
                            stream.BitRate = clipStream.BitRate;
                        }
                        stream.IsVBR = clipStream.IsVBR;

                        if (stream.IsVideoStream &&
                            clipStream.IsVideoStream)
                        {
                            ((TSVideoStream)stream).EncodingProfile =
                                ((TSVideoStream)clipStream).EncodingProfile;
                        }
                        else if (stream.IsAudioStream &&
                            clipStream.IsAudioStream)
                        {
                            TSAudioStream audioStream = (TSAudioStream)stream;
                            TSAudioStream clipAudioStream = (TSAudioStream)clipStream;

                            if (clipAudioStream.ChannelCount > audioStream.ChannelCount)
                            {
                                audioStream.ChannelCount = clipAudioStream.ChannelCount;
                            }
                            if (clipAudioStream.LFE > audioStream.LFE)
                            {
                                audioStream.LFE = clipAudioStream.LFE;
                            }
                            if (clipAudioStream.SampleRate > audioStream.SampleRate)
                            {
                                audioStream.SampleRate = clipAudioStream.SampleRate;
                            }
                            if (clipAudioStream.BitDepth > audioStream.BitDepth)
                            {
                                audioStream.BitDepth = clipAudioStream.BitDepth;
                            }
                            if (clipAudioStream.DialNorm < audioStream.DialNorm)
                            {
                                audioStream.DialNorm = clipAudioStream.DialNorm;
                            }
                            if (clipAudioStream.AudioMode != TSAudioMode.Unknown)
                            {
                                audioStream.AudioMode = clipAudioStream.AudioMode;
                            }
                            if (clipAudioStream.CoreStream != null &&
                                audioStream.CoreStream == null)
                            {
                                audioStream.CoreStream = (TSAudioStream)
                                    clipAudioStream.CoreStream.Clone();
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < AngleCount; i++)
            {
                AngleStreams.Add(new Dictionary<ushort, TSStream>());
            }

            if (!BDInfoSettings.KeepStreamOrder)
            {
                VideoStreams.Sort(CompareVideoStreams);
            }
            foreach (TSStream stream in VideoStreams)
            {
                SortedStreams.Add(stream);
                for (int i = 0; i < AngleCount; i++)
                {
                    TSStream angleStream = stream.Clone();
                    angleStream.AngleIndex = i + 1;
                    AngleStreams[i][angleStream.PID] = angleStream;
                    SortedStreams.Add(angleStream);
                }
            }

            if (!BDInfoSettings.KeepStreamOrder)
            {
                AudioStreams.Sort(CompareAudioStreams);
            }
            foreach (TSStream stream in AudioStreams)
            {
                SortedStreams.Add(stream);
            }

            if (!BDInfoSettings.KeepStreamOrder)
            {
                GraphicsStreams.Sort(CompareGraphicsStreams);
            }
            foreach (TSStream stream in GraphicsStreams)
            {
                SortedStreams.Add(stream);
            }

            if (!BDInfoSettings.KeepStreamOrder)
            {
                TextStreams.Sort(CompareTextStreams);
            }
            foreach (TSStream stream in TextStreams)
            {
                SortedStreams.Add(stream);
            }
        }

        public void ClearBitrates()
        {
            foreach (TSStreamClip clip in StreamClips)
            {
                clip.PayloadBytes = 0;
                clip.PacketCount = 0;
                clip.PacketSeconds = 0;

                if (clip.StreamFile != null)
                {
                    foreach (TSStream stream in clip.StreamFile.Streams.Values)
                    {
                        stream.PayloadBytes = 0;
                        stream.PacketCount = 0;
                        stream.PacketSeconds = 0;
                    }

                    if (clip.StreamFile != null &&
                        clip.StreamFile.StreamDiagnostics != null)
                    {
                        clip.StreamFile.StreamDiagnostics.Clear();
                    }
                }
            }

            foreach (TSStream stream in SortedStreams)
            {
                stream.PayloadBytes = 0;
                stream.PacketCount = 0;
                stream.PacketSeconds = 0;
            }
        }

        public bool IsValid
        {
            get
            {
                if (!IsInitialized) return false;

                if (BDInfoSettings.FilterShortPlaylists &&
                    TotalLength < BDInfoSettings.FilterShortPlaylistsValue)
                {
                    return false;
                }

                if (HasLoops &&
                    BDInfoSettings.FilterLoopingPlaylists)
                {
                    return false;
                }

                return true;
            }
        }

        public static int CompareVideoStreams(
            TSVideoStream x, 
            TSVideoStream y)
        {
            if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return 1;
            }
            else if (x != null && y == null)
            {
                return -1;
            }
            else
            {
                if (x.Height > y.Height)
                {
                    return -1;
                }
                else if (y.Height > x.Height)
                {
                    return 1;
                }
                else if (x.PID > y.PID)
                {
                    return 1;
                }
                else if (y.PID > x.PID)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }

        public static int CompareAudioStreams(
            TSAudioStream x, 
            TSAudioStream y)
        {
            if (x == y)
            {
                return 0;
            }
            else if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return -1;
            }
            else if (x != null && y == null)
            {
                return 1;
            }
            else
            {
                if (x.ChannelCount > y.ChannelCount)
                {
                    return -1;
                }
                else if (y.ChannelCount > x.ChannelCount)
                {
                    return 1;
                }
                else
                {
                    int sortX = GetStreamTypeSortIndex(x.StreamType);
                    int sortY = GetStreamTypeSortIndex(y.StreamType);

                    if (sortX > sortY)
                    {
                        return -1;
                    }
                    else if (sortY > sortX)
                    {
                        return 1;
                    }
                    else
                    {
                        if (x.LanguageCode == "eng")
                        {
                            return -1;
                        }
                        else if (y.LanguageCode == "eng")
                        {
                            return 1;
                        }
                        else if (x.LanguageCode != y.LanguageCode)
                        {
                            return string.Compare(
                                x.LanguageName, y.LanguageName);
                        }
                        else if (x.PID < y.PID)
                        {
                            return -1;
                        }
                        else if (y.PID < x.PID)
                        {
                            return 1;
                        }
                        return 0;
                    }
                }
            }
        }

        public static int CompareTextStreams(
            TSTextStream x,
            TSTextStream y)
        {
            if (x == y)
            {
                return 0;
            }
            else if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return -1;
            }
            else if (x != null && y == null)
            {
                return 1;
            }
            else
            {
                if (x.LanguageCode == "eng")
                {
                    return -1;
                }
                else if (y.LanguageCode == "eng")
                {
                    return 1;
                }
                else
                {
                    if (x.LanguageCode == y.LanguageCode)
                    {
                        if (x.PID > y.PID)
                        {
                            return 1;
                        }
                        else if (y.PID > x.PID)
                        {
                            return -1;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return string.Compare(
                            x.LanguageName, y.LanguageName);
                    }
                }
            }
        }

        private static int CompareGraphicsStreams(
            TSGraphicsStream x,
            TSGraphicsStream y)
        {
            if (x == y)
            {
                return 0;
            }
            else if (x == null && y == null)
            {
                return 0;
            }
            else if (x == null && y != null)
            {
                return -1;
            }
            else if (x != null && y == null)
            {
                return 1;
            }
            else
            {
                int sortX = GetStreamTypeSortIndex(x.StreamType);
                int sortY = GetStreamTypeSortIndex(y.StreamType);

                if (sortX > sortY)
                {
                    return -1;
                }
                else if (sortY > sortX)
                {
                    return 1;
                }
                else if (x.LanguageCode == "eng")
                {
                    return -1;
                }
                else if (y.LanguageCode == "eng")
                {
                    return 1;
                }
                else
                {
                    if (x.LanguageCode == y.LanguageCode)
                    {
                        if (x.PID > y.PID)
                        {
                            return 1;
                        }
                        else if (y.PID > x.PID)
                        {
                            return -1;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return string.Compare(x.LanguageName, y.LanguageName);
                    }
                }
            }
        }

        private static int GetStreamTypeSortIndex(TSStreamType streamType)
        {
            switch (streamType)
            {
                case TSStreamType.Unknown:
                    return 0;
                case TSStreamType.MPEG1_VIDEO:
                    return 1;
                case TSStreamType.MPEG2_VIDEO:
                    return 2;
                case TSStreamType.AVC_VIDEO:
                    return 3;
                case TSStreamType.VC1_VIDEO:
                    return 4;
                case TSStreamType.MVC_VIDEO:
                    return 5;

                case TSStreamType.MPEG1_AUDIO:
                    return 1;
                case TSStreamType.MPEG2_AUDIO:
                    return 2;
                case TSStreamType.AC3_PLUS_SECONDARY_AUDIO:
                    return 3;
                case TSStreamType.DTS_HD_SECONDARY_AUDIO:
                    return 4;
                case TSStreamType.AC3_AUDIO:
                    return 5;
                case TSStreamType.DTS_AUDIO:
                    return 6;
                case TSStreamType.AC3_PLUS_AUDIO:
                    return 7;
                case TSStreamType.DTS_HD_AUDIO:
                    return 8;
                case TSStreamType.AC3_TRUE_HD_AUDIO:
                    return 9;
                case TSStreamType.DTS_HD_MASTER_AUDIO:
                    return 10;
                case TSStreamType.LPCM_AUDIO:
                    return 11;

                case TSStreamType.SUBTITLE:
                    return 1;
                case TSStreamType.INTERACTIVE_GRAPHICS:
                    return 2;
                case TSStreamType.PRESENTATION_GRAPHICS:
                    return 3;

                default:
                    return 0;
            }
        }

        protected string ReadString(
            byte[] data,
            int count,
            ref int pos)
        {
            string val =
                _textEncoding.GetASCIIEncoding().GetString(data, pos, count);

            pos += count;

            return val;
        }

        protected int ReadInt32(
            byte[] data,
            ref int pos)
        {
            int val =
                ((int)data[pos] << 24) +
                ((int)data[pos + 1] << 16) +
                ((int)data[pos + 2] << 8) +
                ((int)data[pos + 3]);

            pos += 4;

            return val;
        }

        protected int ReadInt16(
            byte[] data,
            ref int pos)
        {
            int val =
                ((int)data[pos] << 8) +
                ((int)data[pos + 1]);

            pos += 2;

            return val;
        }

        protected byte ReadByte(
            byte[] data,
            ref int pos)
        {
            return data[pos++];
        }
    }
}
