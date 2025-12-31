using System;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Jellyfin.Server.Infrastructure
{
    /// <summary>
    /// An <see cref="IFileProvider"/> wrapper that sanitizes file timestamps to ensure
    /// they are valid Win32 FileTimes.
    /// </summary>
    /// <remarks>
    /// This wrapper prevents <see cref="ArgumentOutOfRangeException"/> in
    /// <see cref="Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware"/> when serving files
    /// with timestamps before 1601-01-01 (the Win32 epoch), which can occur in Docker containers
    /// or on certain filesystems.
    /// </remarks>
    public sealed class SafeTimestampFileProvider : IFileProvider, IDisposable
    {
        private readonly PhysicalFileProvider _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="SafeTimestampFileProvider"/> class.
        /// </summary>
        /// <param name="root">The root directory for this provider.</param>
        public SafeTimestampFileProvider(string root)
        {
            ArgumentNullException.ThrowIfNull(root);
            _inner = new PhysicalFileProvider(root);
        }

        /// <inheritdoc />
        public IFileInfo GetFileInfo(string subpath)
        {
            var fileInfo = _inner.GetFileInfo(subpath);
            return new SafeTimestampFileInfo(fileInfo);
        }

        /// <inheritdoc />
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return _inner.GetDirectoryContents(subpath);
        }

        /// <inheritdoc />
        public IChangeToken Watch(string filter)
        {
            return _inner.Watch(filter);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _inner.Dispose();
        }
    }
}
