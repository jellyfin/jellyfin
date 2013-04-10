using MediaBrowser.Controller.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace MediaBrowser.Controller.Localization
{
    /// <summary>
    /// Class RatingsDefinition
    /// </summary>
    public class RatingsDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RatingsDefinition" /> class.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        public RatingsDefinition(string file, IServerConfigurationManager configurationManager)
        {
            this.file = file;
            if (!Load())
            {
                Init(configurationManager.Configuration.MetadataCountryCode.ToUpper());
            }
        }

        /// <summary>
        /// Inits the specified country.
        /// </summary>
        /// <param name="country">The country.</param>
        protected void Init(string country)
        {
            //intitialze based on country
            switch (country)
            {
                case "US":
                    RatingsDict = new USRatingsDictionary();
                    break;
                case "GB":
                    RatingsDict = new GBRatingsDictionary();
                    break;
                case "NL":
                    RatingsDict = new NLRatingsDictionary();
                    break;
                case "AU":
                    RatingsDict = new AURatingsDictionary();
                    break;
                default:
                    RatingsDict = new USRatingsDictionary();
                    break;
            }
            Save();
        }

        /// <summary>
        /// The file
        /// </summary>
        readonly string file;

        /// <summary>
        /// Save to file
        /// </summary>
        public void Save()
        {
            // Use simple text serialization - no need for xml
            using (var fs = new StreamWriter(file))
            {
                foreach (var pair in RatingsDict)
                {
                    fs.WriteLine(pair.Key + "," + pair.Value);
                }
            }
        }

        /// <summary>
        /// Load from file
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected bool Load()
        {
            // Read back in our simple serialized format
            RatingsDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var fs = new StreamReader(file))
                {
                    while (!fs.EndOfStream)
                    {
                        var line = fs.ReadLine() ?? "";
                        var values = line.Split(',');
                        if (values.Length == 2)
                        {

                            int value;

                            if (int.TryParse(values[1], out value))
                            {
                                RatingsDict[values[0].Trim()] = value;
                            }
                            else
                            {
                                //Logger.Error("Invalid line in ratings file " + file + "(" + line + ")");
                            }
                        }
                    }
                }
            }
            catch
            {
                // Couldn't load - probably just not there yet
                return false;
            }
            return true;
        }

        /// <summary>
        /// The ratings dict
        /// </summary>
        public Dictionary<string, int> RatingsDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    }
}
