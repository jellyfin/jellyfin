using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BDInfo;
using DiscUtils.Udf;
using Jellyfin.Extensions;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.BdInfo;

/// <summary>
/// Class BdInfoExaminer.
/// </summary>
public class BdInfoExaminer : IBlurayExaminer
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="BdInfoExaminer" /> class.
    /// </summary>
    /// <param name="fileSystem">The filesystem.</param>
    public BdInfoExaminer(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <summary>
    /// Gets the disc info.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>BlurayDiscInfo.</returns>
    public BlurayDiscInfo GetDiscInfo(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(nameof(path));
        }

        var fileSystemInfo = _fileSystem.GetFileSystemInfo(path);
        if (fileSystemInfo.Exists && !fileSystemInfo.IsDirectory)
        {
            return GetDiscInfoFromIso(path);
        }

        var bdrom = new BDROM(BdInfoDirectoryInfo.FromFileSystemPath(_fileSystem, path));
        return GetDiscInfo(bdrom, includePlaylistFiles: true);
    }

    private static BlurayDiscInfo GetDiscInfoFromIso(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var udfReader = new UdfReader(stream);

        var bdmvDirectory = udfReader.Root.GetDirectories()
            .FirstOrDefault(directory => string.Equals(directory.Name, "BDMV", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("The ISO image does not contain a BDMV directory.");

        var bdrom = new BDROM(new BdInfoUdfDirectoryInfo(bdmvDirectory));
        var discInfo = GetDiscInfo(bdrom, includePlaylistFiles: false);
        discInfo.IsIso = true;
        return discInfo;
    }

    private static BlurayDiscInfo GetDiscInfo(BDROM bdrom, bool includePlaylistFiles)
    {
        bdrom.Scan();

        // Get the longest playlist
        var playlist = bdrom.PlaylistFiles.Values.OrderByDescending(p => p.TotalLength).FirstOrDefault(p => p.IsValid);

        var outputStream = new BlurayDiscInfo
        {
            Chapters = Array.Empty<double>(),
            Files = Array.Empty<string>(),
            MediaStreams = Array.Empty<MediaStream>()
        };

        if (playlist is null)
        {
            return outputStream;
        }

        outputStream.Chapters = playlist.Chapters.ToArray();

        outputStream.RunTimeTicks = TimeSpan.FromSeconds(playlist.TotalLength).Ticks;

        var sortedStreams = playlist.SortedStreams;
        var mediaStreams = new List<MediaStream>(sortedStreams.Count);

        for (int i = 0; i < sortedStreams.Count; i++)
        {
            var stream = sortedStreams[i];
            switch (stream)
            {
                case TSVideoStream videoStream:
                    AddVideoStream(mediaStreams, i, videoStream);
                    break;
                case TSAudioStream audioStream:
                    AddAudioStream(mediaStreams, i, audioStream);
                    break;
                case TSTextStream:
                case TSGraphicsStream:
                    AddSubtitleStream(mediaStreams, i, stream);
                    break;
            }
        }

        outputStream.MediaStreams = mediaStreams.ToArray();

        outputStream.PlaylistName = playlist.Name;

        if (includePlaylistFiles && playlist.StreamClips is not null)
        {
            // Get the files in the playlist
            outputStream.Files = playlist.StreamClips
                .Where(i => i.AngleIndex == 0 && i.StreamFile is not null)
                .Select(i => i.StreamFile.FileInfo.FullName)
                .ToArray();
        }

        return outputStream;
    }

    /// <summary>
    /// Adds the video stream.
    /// </summary>
    /// <param name="streams">The streams.</param>
    /// <param name="index">The stream index.</param>
    /// <param name="videoStream">The video stream.</param>
    private static void AddVideoStream(List<MediaStream> streams, int index, TSVideoStream videoStream)
    {
        var mediaStream = new MediaStream
        {
            BitRate = Convert.ToInt32(videoStream.BitRate),
            Width = videoStream.Width,
            Height = videoStream.Height,
            Codec = GetNormalizedCodec(videoStream),
            IsInterlaced = videoStream.IsInterlaced,
            Type = MediaStreamType.Video,
            Index = index
        };

        if (videoStream.FrameRateDenominator > 0)
        {
            float frameRateEnumerator = videoStream.FrameRateEnumerator;
            float frameRateDenominator = videoStream.FrameRateDenominator;

            mediaStream.AverageFrameRate = mediaStream.RealFrameRate = frameRateEnumerator / frameRateDenominator;
        }

        streams.Add(mediaStream);
    }

    /// <summary>
    /// Adds the audio stream.
    /// </summary>
    /// <param name="streams">The streams.</param>
    /// <param name="index">The stream index.</param>
    /// <param name="audioStream">The audio stream.</param>
    private static void AddAudioStream(List<MediaStream> streams, int index, TSAudioStream audioStream)
    {
        var stream = new MediaStream
        {
            Codec = GetNormalizedCodec(audioStream),
            Language = audioStream.LanguageCode,
            ChannelLayout = string.Format(CultureInfo.InvariantCulture, "{0:D}.{1:D}", audioStream.ChannelCount, audioStream.LFE),
            Channels = audioStream.ChannelCount + audioStream.LFE,
            SampleRate = audioStream.SampleRate,
            Type = MediaStreamType.Audio,
            Index = index
        };

        var bitrate = Convert.ToInt32(audioStream.BitRate);

        if (bitrate > 0)
        {
            stream.BitRate = bitrate;
        }

        streams.Add(stream);
    }

    /// <summary>
    /// Adds the subtitle stream.
    /// </summary>
    /// <param name="streams">The streams.</param>
    /// <param name="index">The stream index.</param>
    /// <param name="stream">The stream.</param>
    private static void AddSubtitleStream(List<MediaStream> streams, int index, TSStream stream)
    {
        streams.Add(new MediaStream
        {
            Language = stream.LanguageCode,
            Codec = GetNormalizedCodec(stream),
            Type = MediaStreamType.Subtitle,
            Index = index
        });
    }

    private static string GetNormalizedCodec(TSStream stream)
        => stream.StreamType switch
        {
            TSStreamType.MPEG1_VIDEO => "mpeg1video",
            TSStreamType.MPEG2_VIDEO => "mpeg2video",
            TSStreamType.VC1_VIDEO => "vc1",
            TSStreamType.AC3_PLUS_AUDIO or TSStreamType.AC3_PLUS_SECONDARY_AUDIO => "eac3",
            TSStreamType.DTS_AUDIO or TSStreamType.DTS_HD_AUDIO or TSStreamType.DTS_HD_MASTER_AUDIO or TSStreamType.DTS_HD_SECONDARY_AUDIO => "dts",
            TSStreamType.PRESENTATION_GRAPHICS => "pgssub",
            _ => stream.CodecShortName
        };
}
