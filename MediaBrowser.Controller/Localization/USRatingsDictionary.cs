using System.Collections.Generic;

namespace MediaBrowser.Controller.Localization
{
    /// <summary>
    /// Class USRatingsDictionary
    /// </summary>
    public class USRatingsDictionary : Dictionary<string,int>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="USRatingsDictionary" /> class.
        /// </summary>
        public USRatingsDictionary()
        {
            Add("G", 1);
            Add("E", 1);
            Add("EC", 1);
            Add("TV-G", 1);
            Add("TV-Y", 2);
            Add("TV-Y7", 3);
            Add("TV-Y7-FV", 4);
            Add("PG", 5);
            Add("TV-PG", 5);
            Add("PG-13", 7);
            Add("T", 7);
            Add("TV-14", 8);
            Add("R", 9);
            Add("M", 9);
            Add("TV-MA", 9);
            Add("NC-17", 10);
            Add("AO", 15);
            Add("RP", 15);
            Add("UR", 15);
            Add("NR", 15);
            Add("X", 15);
            Add("XXX", 100);
        }
    }
}
