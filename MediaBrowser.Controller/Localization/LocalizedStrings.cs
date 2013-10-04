using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MediaBrowser.Controller.Localization
{
    /// <summary>
    /// Class LocalizedStrings
    /// </summary>
    public class LocalizedStrings
    {
        public static IServerApplicationPaths ApplicationPaths;

        /// <summary>
        /// Gets the list of Localized string files
        /// </summary>
        /// <value>The string files.</value>
        public static IEnumerable<LocalizedStringData> StringFiles { get; set; }

        /// <summary>
        /// The base prefix
        /// </summary>
        public const string BasePrefix = "base-";
        /// <summary>
        /// The local strings
        /// </summary>
        protected ConcurrentDictionary<string, string> LocalStrings = new ConcurrentDictionary<string, string>();
        /// <summary>
        /// The _instance
        /// </summary>
        private static LocalizedStrings _instance;

        private readonly IServerApplicationPaths _appPaths;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static LocalizedStrings Instance { get { return _instance ?? (_instance = new LocalizedStrings(ApplicationPaths)); } }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizedStrings" /> class.
        /// </summary>
        public LocalizedStrings(IServerApplicationPaths appPaths)
        {
            _appPaths = appPaths;

            foreach (var stringObject in StringFiles)
            {
                AddStringData(LoadFromFile(GetFileName(stringObject),stringObject.GetType()));
            }
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        /// <param name="stringObject">The string object.</param>
        /// <returns>System.String.</returns>
        protected string GetFileName(LocalizedStringData stringObject)
        {
            var path = _appPaths.LocalizationPath;
            var name = Path.Combine(path, stringObject.Prefix + "strings-" + CultureInfo.CurrentCulture + ".xml");
            if (File.Exists(name))
            {
                return name;
            }

            name = Path.Combine(path, stringObject.Prefix + "strings-" + CultureInfo.CurrentCulture.Parent + ".xml");
            if (File.Exists(name))
            {
                return name;
            }

            //just return default
            return Path.Combine(path, stringObject.Prefix + "strings-en.xml");
        }

        /// <summary>
        /// Loads from file.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="t">The t.</param>
        /// <returns>LocalizedStringData.</returns>
        protected LocalizedStringData LoadFromFile(string file, Type t)
        {
            return new BaseStrings {FileName = file};
            //var xs = new XmlSerializer(t);
            //var strings = (LocalizedStringData)Activator.CreateInstance(t);
            //strings.FileName = file;
            //Logger.Info("Using String Data from {0}", file);
            //if (File.Exists(file))
            //{
            //    using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
            //    {
            //        strings = (LocalizedStringData)xs.Deserialize(fs);
            //    }
            //}
            //else
            //{
            //    strings.Save(); //brand new - save it
            //}

            //if (strings.ThisVersion != strings.Version && file.ToLower().Contains("-en.xml"))
            //{
            //    //only re-save the english version as that is the one defined internally
            //    strings = new BaseStrings {FileName = file};
            //    strings.Save();
            //}
            //return strings;

        }

        /// <summary>
        /// Adds the string data.
        /// </summary>
        /// <param name="stringData">The string data.</param>
        public void AddStringData(object stringData )
        {
            //translate our object definition into a dictionary for lookups
            // and a reverse dictionary so we can lookup keys by value
            foreach (var field in stringData.GetType().GetFields().Where(f => f != null && f.FieldType == typeof(string)))
            {
                string value;

                try
                {
                    value = field.GetValue(stringData) as string;
                }
                catch (TargetException)
                {
                    //Logger.ErrorException("Error getting value for field: {0}", ex, field.Name);
                    continue;
                }
                catch (FieldAccessException)
                {
                    //Logger.ErrorException("Error getting value for field: {0}", ex, field.Name);
                    continue;
                }
                catch (NotSupportedException)
                {
                    //Logger.ErrorException("Error getting value for field: {0}", ex, field.Name);
                    continue;
                }

                LocalStrings.TryAdd(field.Name, value);
            }
        }

        /// <summary>
        /// Gets the string.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        public string GetString(string key)
        {
            string value;

            LocalStrings.TryGetValue(key, out value);
            return value;
        }
    }
}
