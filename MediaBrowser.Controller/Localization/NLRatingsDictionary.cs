using System.Collections.Generic;

namespace MediaBrowser.Controller.Localization
{
    /// <summary>
    /// Class NLRatingsDictionary
    /// </summary>
    public class NLRatingsDictionary : Dictionary<string, int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NLRatingsDictionary" /> class.
        /// </summary>
        public NLRatingsDictionary()
        {
            Add("NL-AL", 1);
            Add("NL-MG6", 2);
            Add("NL-6", 3);
            Add("NL-9", 5);
            Add("NL-12", 6);
            Add("NL-16", 8);
        }
    }
}
