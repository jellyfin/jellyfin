using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Jellyfin.Server.Infrastructure
{
    /// <summary>
    /// An <see cref="IFileInfo"/> wrapper that sanitizes <see cref="LastModified"/> timestamps
    /// to ensure they are valid Win32 FileTimes.
    /// </summary>
    /// <remarks>
    /// This wrapper prevents <see cref="ArgumentOutOfRangeException"/> in
    /// <see cref="Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware"/> when serving files
    /// with timestamps before 1601-01-01 (the Win32 epoch), which can occur in Docker containers
    /// or on certain filesystems.
    /// </remarks>
    public class SafeTimestampFileInfo : IFileInfo
    {
        /// <summary>
        /// The minimum valid Win32 FileTime is 1601-01-01. We use 1601-01-02 for safety margin.
        /// </summary>
        private static readonly DateTimeOffset _minValidWin32Time =
            new DateTimeOffset(1601, 1, 2, 0, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// Safe fallback timestamp (Unix epoch: 1970-01-01).
        /// </summary>
        private static readonly DateTimeOffset _safeFallbackTime =
            new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        private readonly IFileInfo _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeTimestampFileInfo"/> class.
        /// </summary>
        /// <param name="inner">The inner <see cref="IFileInfo"/> to wrap.</param>
        public SafeTimestampFileInfo(IFileInfo inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <inheritdoc />
        public bool Exists => _inner.Exists;

        /// <inheritdoc />
        public long Length => _inner.Length;

        /// <inheritdoc />
        public string? PhysicalPath => _inner.PhysicalPath;

        /// <inheritdoc />
        public string Name => _inner.Name;

        /// <inheritdoc />
        /// <remarks>
        /// Returns the original timestamp if valid, otherwise returns 1970-01-01 (Unix epoch).
        /// </remarks>
        public DateTimeOffset LastModified =>
            _inner.LastModified < _minValidWin32Time ? _safeFallbackTime : _inner.LastModified;

        /// <inheritdoc />
        public bool IsDirectory => _inner.IsDirectory;

        /// <inheritdoc />
        public Stream CreateReadStream() => _inner.CreateReadStream();
    }
}
