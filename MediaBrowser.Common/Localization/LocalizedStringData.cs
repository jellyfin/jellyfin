using System.IO;
using System.Xml.Serialization;

namespace MediaBrowser.Common.Localization
{
    /// <summary>
    /// Class LocalizedStringData
    /// </summary>
    public class LocalizedStringData
    {
        /// <summary>
        /// The this version
        /// </summary>
        [XmlIgnore]
        public string ThisVersion = "1.0000";
        /// <summary>
        /// The prefix
        /// </summary>
        [XmlIgnore]
        public string Prefix = "";
        /// <summary>
        /// The file name
        /// </summary>
        public string FileName; //this is public so it will serialize and we know where to save ourselves
        /// <summary>
        /// The version
        /// </summary>
        public string Version = ""; //this will get saved so we can check it against us for changes

        /// <summary>
        /// Saves this instance.
        /// </summary>
        public void Save()
        {
            Save(FileName);
        }

        /// <summary>
        /// Saves the specified file.
        /// </summary>
        /// <param name="file">The file.</param>
        public void Save(string file)
        {
            var xs = new XmlSerializer(GetType());
            using (var fs = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                xs.Serialize(fs, this);
            }
        }
    }
}
