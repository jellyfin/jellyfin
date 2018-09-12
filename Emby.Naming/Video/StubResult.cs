using System.Collections.Generic;

namespace Emby.Naming.Video
{
    public class StubResult
    {
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
        /// Gets or sets the tokens.
        /// </summary>
        /// <value>The tokens.</value>
        public List<string> Tokens { get; set; }

        public StubResult()
        {
            Tokens = new List<string>();
        }
    }
}
