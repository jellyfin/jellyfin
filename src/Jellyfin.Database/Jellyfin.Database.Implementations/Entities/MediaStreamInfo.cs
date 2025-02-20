#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Data.Entities;

public class MediaStreamInfo
{
    public required Guid ItemId { get; set; }

    public required BaseItemEntity Item { get; set; }

    public int StreamIndex { get; set; }

    public required MediaStreamTypeEntity StreamType { get; set; }

    public string? Codec { get; set; }

    public string? Language { get; set; }

    public string? ChannelLayout { get; set; }

    public string? Profile { get; set; }

    public string? AspectRatio { get; set; }

    public string? Path { get; set; }

    public bool? IsInterlaced { get; set; }

    public int? BitRate { get; set; }

    public int? Channels { get; set; }

    public int? SampleRate { get; set; }

    public bool IsDefault { get; set; }

    public bool IsForced { get; set; }

    public bool IsExternal { get; set; }

    public int? Height { get; set; }

    public int? Width { get; set; }

    public float? AverageFrameRate { get; set; }

    public float? RealFrameRate { get; set; }

    public float? Level { get; set; }

    public string? PixelFormat { get; set; }

    public int? BitDepth { get; set; }

    public bool? IsAnamorphic { get; set; }

    public int? RefFrames { get; set; }

    public string? CodecTag { get; set; }

    public string? Comment { get; set; }

    public string? NalLengthSize { get; set; }

    public bool? IsAvc { get; set; }

    public string? Title { get; set; }

    public string? TimeBase { get; set; }

    public string? CodecTimeBase { get; set; }

    public string? ColorPrimaries { get; set; }

    public string? ColorSpace { get; set; }

    public string? ColorTransfer { get; set; }

    public int? DvVersionMajor { get; set; }

    public int? DvVersionMinor { get; set; }

    public int? DvProfile { get; set; }

    public int? DvLevel { get; set; }

    public int? RpuPresentFlag { get; set; }

    public int? ElPresentFlag { get; set; }

    public int? BlPresentFlag { get; set; }

    public int? DvBlSignalCompatibilityId { get; set; }

    public bool? IsHearingImpaired { get; set; }

    public int? Rotation { get; set; }

    public string? KeyFrames { get; set; }
}
