using System;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Data.Entities;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class MediaStreamInfo
{
    public required Guid ItemId { get; set; }

    public required BaseItemEntity Item { get; set; }

    public int StreamIndex { get; set; }

    public MediaStreamTypeEntity? StreamType { get; set; }

    public string? Codec { get; set; }

    public string? Language { get; set; }

    public string? ChannelLayout { get; set; }

    public string? Profile { get; set; }

    public string? AspectRatio { get; set; }

    public string? Path { get; set; }

    public bool IsInterlaced { get; set; }

    public required int BitRate { get; set; }

    public required int Channels { get; set; }

    public required int SampleRate { get; set; }

    public bool IsDefault { get; set; }

    public bool IsForced { get; set; }

    public bool IsExternal { get; set; }

    public required int Height { get; set; }

    public required int Width { get; set; }

    public required float AverageFrameRate { get; set; }

    public required float RealFrameRate { get; set; }

    public required float Level { get; set; }

    public string? PixelFormat { get; set; }

    public required int BitDepth { get; set; }

    public required bool IsAnamorphic { get; set; }

    public required int RefFrames { get; set; }

    public required string CodecTag { get; set; }

    public required string Comment { get; set; }

    public required string NalLengthSize { get; set; }

    public required bool IsAvc { get; set; }

    public required string Title { get; set; }

    public required string TimeBase { get; set; }

    public required string CodecTimeBase { get; set; }

    public required string ColorPrimaries { get; set; }

    public required string ColorSpace { get; set; }

    public required string ColorTransfer { get; set; }

    public required int DvVersionMajor { get; set; }

    public required int DvVersionMinor { get; set; }

    public required int DvProfile { get; set; }

    public required int DvLevel { get; set; }

    public required int RpuPresentFlag { get; set; }

    public required int ElPresentFlag { get; set; }

    public required int BlPresentFlag { get; set; }

    public required int DvBlSignalCompatibilityId { get; set; }

    public required bool IsHearingImpaired { get; set; }

    public required int Rotation { get; set; }

    public string? KeyFrames { get; set; }
}
