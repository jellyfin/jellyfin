using System;
using MediaBrowser.Model.Entities;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Represents a single video file.
    /// </summary>
    public class VideoFileInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoFileInfo"/> class.
        /// </summary>
        /// <param name="name">Name of file.</param>
        /// <param name="path">Path to the file.</param>
        /// <param name="container">Container type.</param>
        /// <param name="year">Year of release.</param>
        /// <param name="extraType">Extra type.</param>
        /// <param name="extraRule">Extra rule.</param>
        /// <param name="format3D">Format 3D.</param>
        /// <param name="is3D">Is 3D.</param>
        /// <param name="isStub">Is Stub.</param>
        /// <param name="stubType">Stub type.</param>
        /// <param name="isDirectory">Is directory.</param>
        public VideoFileInfo(string name, string path, string? container, int? year = default, ExtraType? extraType = default, ExtraRule? extraRule = default, string? format3D = default, bool is3D = default, bool isStub = default, string? stubType = default, bool isDirectory = default)
        {
            Path = path;
            Container = container;
            Name = name;
            Year = year;
            ExtraType = extraType;
            ExtraRule = extraRule;
            Format3D = format3D;
            Is3D = is3D;
            IsStub = isStub;
            StubType = stubType;
            IsDirectory = isDirectory;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the container.
        /// </summary>
        /// <value>The container.</value>
        public string? Container { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        /// <value>The year.</value>
        public int? Year { get; set; }

        /// <summary>
        /// Gets or sets the type of the extra, e.g. trailer, theme song, behind the scenes, etc.
        /// </summary>
        /// <value>The type of the extra.</value>
        public ExtraType? ExtraType { get; set; }

        /// <summary>
        /// Gets or sets the extra rule.
        /// </summary>
        /// <value>The extra rule.</value>
        public ExtraRule? ExtraRule { get; set; }

        /// <summary>
        /// Gets or sets the format3 d.
        /// </summary>
        /// <value>The format3 d.</value>
        public string? Format3D { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [is3 d].
        /// </summary>
        /// <value><c>true</c> if [is3 d]; otherwise, <c>false</c>.</value>
        public bool Is3D { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is stub.
        /// </summary>
        /// <value><c>true</c> if this instance is stub; otherwise, <c>false</c>.</value>
        public bool IsStub { get; set; }

        /// <summary>
        /// Gets or sets the type of the stub.
        /// </summary>
        /// <value>The type of the stub.</value>
        public string? StubType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a directory.
        /// </summary>
        /// <value>The type.</value>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Gets the file name without extension.
        /// </summary>
        /// <value>The file name without extension.</value>
        public ReadOnlySpan<char> FileNameWithoutExtension => !IsDirectory
            ? System.IO.Path.GetFileNameWithoutExtension(Path.AsSpan())
            : System.IO.Path.GetFileName(Path.AsSpan());

        /// <inheritdoc />
        public override string ToString()
        {
            return "VideoFileInfo(Name: '" + Name + "')";
        }
    }
}
