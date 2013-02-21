using System.Collections.Generic;

namespace MediaBrowser.Controller.Localization
{
    /// <summary>
    /// Class AURatingsDictionary
    /// </summary>
    public class AURatingsDictionary : Dictionary<string, int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AURatingsDictionary" /> class.
        /// </summary>
        public AURatingsDictionary()
        {
            Add("AU-G", 1);
            Add("AU-PG", 5);
            Add("AU-M", 6);
            Add("AU-M15+", 7);
            Add("AU-R18+", 9);
            Add("AU-X18+", 10);
        }
    }
}
