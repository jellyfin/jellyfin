using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;

namespace Jellyfin.Server.Infrastructure.FileTransformation;

internal class TransformableFileInfo : IFileInfo
{
    private readonly PhysicalFileInfo _baseInfo;
    private readonly Stream _transformedStream;

    public TransformableFileInfo(PhysicalFileInfo baseInfo, Stream transformedStream)
    {
        _baseInfo = baseInfo;
        _transformedStream = transformedStream;
    }

    public bool Exists => _baseInfo.Exists;

    public bool IsDirectory => _baseInfo.IsDirectory;

    public DateTimeOffset LastModified => _baseInfo.LastModified;

    public long Length => _transformedStream.Length;

    public string Name => _baseInfo.Name;

    public string? PhysicalPath => null;

    public Stream CreateReadStream()
    {
        return _transformedStream;
    }
}
