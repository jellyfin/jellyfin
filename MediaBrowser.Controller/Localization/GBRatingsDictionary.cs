using System.Collections.Generic;

namespace MediaBrowser.Controller.Localization
{
    /// <summary>
    /// Class GBRatingsDictionary
    /// </summary>
    public class GBRatingsDictionary : Dictionary<string, int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GBRatingsDictionary" /> class.
        /// </summary>
        public GBRatingsDictionary()
        {
            Add("GB-U", 1);
            Add("GB-PG", 5);
            Add("GB-12", 6);
            Add("GB-12A", 7);
            Add("GB-15", 8);
            Add("GB-18", 9);
            Add("GB-R18", 15);
        }
    }
}
