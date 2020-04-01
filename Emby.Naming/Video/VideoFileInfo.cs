using MediaBrowser.Model.Entities;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Represents a single video file.
    /// </summary>
    public class VideoFileInfo
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the container.
        /// </summary>
        /// <value>The container.</value>
        public string Container { get; set; }

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
        public ExtraRule ExtraRule { get; set; }

        /// <summary>
        /// Gets or sets the format3 d.
        /// </summary>
        /// <value>The format3 d.</value>
        public string Format3D { get; set; }

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
        public string StubType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a directory.
        /// </summary>
        /// <value>The type.</value>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Gets the file name without extension.
        /// </summary>
        /// <value>The file name without extension.</value>
        public string FileNameWithoutExtension => !IsDirectory
            ? System.IO.Path.GetFileNameWithoutExtension(Path)
            : System.IO.Path.GetFileName(Path);

        /// <inheritdoc />
        public override string ToString()
        {
            // Makes debugging easier
            return Name ?? base.ToString();
        }
    }
}
