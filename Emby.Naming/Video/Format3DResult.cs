using System.Collections.Generic;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Helper object to return data from <see cref="Format3DParser"/>.
    /// </summary>
    public class Format3DResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Format3DResult"/> class.
        /// </summary>
        public Format3DResult()
        {
            Tokens = new List<string>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether [is3 d].
        /// </summary>
        /// <value><c>true</c> if [is3 d]; otherwise, <c>false</c>.</value>
        public bool Is3D { get; set; }

        /// <summary>
        /// Gets or sets the format3 d.
        /// </summary>
        /// <value>The format3 d.</value>
        public string? Format3D { get; set; }

        /// <summary>
        /// Gets or sets the tokens.
        /// </summary>
        /// <value>The tokens.</value>
        public List<string> Tokens { get; set; }
    }
}
