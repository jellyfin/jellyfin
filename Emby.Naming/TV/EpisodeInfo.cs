#pragma warning disable CS1591

namespace Emby.Naming.TV
{
    public class EpisodeInfo
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
        /// Gets or sets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        public string SeriesName { get; set; }

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

        public int? SeasonNumber { get; set; }

        public int? EpisodeNumber { get; set; }

        public int? EndingEpsiodeNumber { get; set; }

        public int? Year { get; set; }

        public int? Month { get; set; }

        public int? Day { get; set; }

        public bool IsByDate { get; set; }
    }
}
