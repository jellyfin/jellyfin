#pragma warning disable CS1591

using System.Collections.Generic;

namespace Emby.Naming.Video
{
    public class Format3DResult
    {
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
        public string Format3D { get; set; }

        /// <summary>
        /// Gets or sets the tokens.
        /// </summary>
        /// <value>The tokens.</value>
        public List<string> Tokens { get; set; }
    }
}
