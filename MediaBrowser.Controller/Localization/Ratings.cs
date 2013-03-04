using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Controller.Localization
{
    /// <summary>
    /// Class Ratings
    /// </summary>
    public static class Ratings
    {
        internal static IServerConfigurationManager ConfigurationManager;

        /// <summary>
        /// The ratings def
        /// </summary>
        private static RatingsDefinition ratingsDef;
        /// <summary>
        /// The _ratings dict
        /// </summary>
        private static Dictionary<string, int> _ratingsDict;
        /// <summary>
        /// Gets the ratings dict.
        /// </summary>
        /// <value>The ratings dict.</value>
        public static Dictionary<string, int> RatingsDict
        {
            get { return _ratingsDict ?? (_ratingsDict = Initialize(false, ConfigurationManager)); }
        }
        /// <summary>
        /// The ratings strings
        /// </summary>
        private static readonly Dictionary<int, string> ratingsStrings = new Dictionary<int, string>();

        /// <summary>
        /// Initializes the specified block unrated.
        /// </summary>
        /// <param name="blockUnrated">if set to <c>true</c> [block unrated].</param>
        /// <returns>Dictionary{System.StringSystem.Int32}.</returns>
        public static Dictionary<string, int> Initialize(bool blockUnrated, IServerConfigurationManager configurationManager)
        {
            //build our ratings dictionary from the combined local one and us one
            ratingsDef = new RatingsDefinition(Path.Combine(configurationManager.ApplicationPaths.LocalizationPath, "Ratings-" + configurationManager.Configuration.MetadataCountryCode + ".txt"), configurationManager);
            //global value of None
            var dict = new Dictionary<string, int> {{"None", -1}};
            foreach (var pair in ratingsDef.RatingsDict)
            {
                dict.TryAdd(pair.Key, pair.Value);
            }
            if (configurationManager.Configuration.MetadataCountryCode.ToUpper() != "US")
            {
                foreach (var pair in new USRatingsDictionary())
                {
                    dict.TryAdd(pair.Key, pair.Value);
                }
            }
            //global values of CS
            dict.TryAdd("CS", 1000);

            dict.TryAdd("", blockUnrated ? 1000 : 0);

            //and rating reverse lookup dictionary (non-redundant ones)
            ratingsStrings.Clear();
            var lastLevel = -10;
            ratingsStrings.Add(-1,LocalizedStrings.Instance.GetString("Any"));
            foreach (var pair in ratingsDef.RatingsDict.OrderBy(p => p.Value))
            {
                if (pair.Value > lastLevel)
                {
                    lastLevel = pair.Value;
                    ratingsStrings.TryAdd(pair.Value, pair.Key);
                }
            }

            ratingsStrings.TryAdd(999, "CS");

            return dict;
        }

        /// <summary>
        /// Switches the unrated.
        /// </summary>
        /// <param name="block">if set to <c>true</c> [block].</param>
        public static void SwitchUnrated(bool block)
        {
            RatingsDict.Remove("");
            RatingsDict.Add("", block ? 1000 : 0);
        }

        /// <summary>
        /// Levels the specified rating STR.
        /// </summary>
        /// <param name="ratingStr">The rating STR.</param>
        /// <returns>System.Int32.</returns>
        public static int Level(string ratingStr)
        {
            if (ratingStr == null) ratingStr = "";
            if (RatingsDict.ContainsKey(ratingStr))
                return RatingsDict[ratingStr];

            string stripped = StripCountry(ratingStr);
            if (RatingsDict.ContainsKey(stripped))
                return RatingsDict[stripped];

            return RatingsDict[""]; //return "unknown" level
        }

        /// <summary>
        /// Strips the country.
        /// </summary>
        /// <param name="rating">The rating.</param>
        /// <returns>System.String.</returns>
        private static string StripCountry(string rating)
        {
            int start = rating.IndexOf('-');
            return start > 0 ? rating.Substring(start + 1) : rating;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public static string ToString(int level)
        {
            //return the closest one
            while (level > 0) 
            {
                if (ratingsStrings.ContainsKey(level))
                    return ratingsStrings[level];

                level--;
            }
            return ratingsStrings.Values.FirstOrDefault(); //default to first one
        }
        /// <summary>
        /// To the strings.
        /// </summary>
        /// <returns>List{System.String}.</returns>
        public static List<string> ToStrings()
        {
            //return the whole list of ratings strings
            return ratingsStrings.Values.ToList();
        }

        /// <summary>
        /// To the values.
        /// </summary>
        /// <returns>List{System.Int32}.</returns>
        public static List<int> ToValues()
        {
            //return the whole list of ratings values
            return ratingsStrings.Keys.ToList();
        }

        //public Microsoft.MediaCenter.UI.Image RatingImage(string rating)
        //{
        //    return Helper.GetMediaInfoImage("Rated_" + rating);
        //}


    }
}
