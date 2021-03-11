using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Defines the <see cref="XmlConfigMigrationHelper" />.
    /// </summary>
    public static class XmlConfigMigrationHelper
    {
        /// <summary>
        /// Processes the invalid entries in an XML file, and migrates them into a new file.
        /// The type <typeparamref name="T"/> should <b>not</b> contain the properties that need to be migrated.
        /// </summary>
        /// <typeparam name="T">The type of the existing configuration class with the obsolete properties <b>removed</b>.</typeparam>
        /// <typeparam name="T2">The type of the destination configuration class.</typeparam>
        /// <param name="sourceFile">The source xml file to be migrated.</param>
        /// <param name="destFile">The destination xml to be created based upon <typeparamref name="T2"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/> instance.</param>
        /// <param name="xmlSerializer">The <see cref="IXmlSerializer"/> instance.</param>
        public static void MigrateConfigurationTo<T, T2>(string sourceFile, string destFile, ILogger logger, IXmlSerializer xmlSerializer)
        {
            if (!File.Exists(destFile))
            {
                var settings = Activator.CreateInstance<T2>();
                var settingsType = typeof(T2);
                var props = settingsType.GetProperties().Where(x => x.CanWrite).ToList();

                // manually load source xml file.
                var serializer = new XmlSerializer(typeof(T));
                serializer.UnknownElement += (object? sender, XmlElementEventArgs e) =>
                {
                    var p = props.Find(x => x.Name == e.Element.Name);
                    if (p != null)
                    {
                        if (p.PropertyType == typeof(bool))
                        {
                            bool.TryParse(e.Element.InnerText, out var boolVal);
                            p.SetValue(settings, boolVal);
                        }
                        else if (p.PropertyType == typeof(int))
                        {
                            int.TryParse(e.Element.InnerText, out var intVal);
                            p.SetValue(settings, intVal);
                        }
                        else if (p.PropertyType == typeof(string[]))
                        {
                            var items = new List<string>();
                            foreach (XmlNode el in e.Element.ChildNodes)
                            {
                                items.Add(el.InnerText);
                            }

                            p.SetValue(settings, items.ToArray());
                        }
                        else
                        {
                            try
                            {
                                p.SetValue(settings, e.Element.InnerText ?? string.Empty);
                            }
                            catch
                            {
                                logger.LogError(
                                    "Unable to migrate value {Name}. Unknown datatype {Type}. Value {Value}.",
                                    e.Element.Name,
                                    p.PropertyType.ToString(),
                                    e.Element.InnerText);
                            }
                        }
                    }
                };

                T? deserialized;
                try
                {
                    using (StreamReader reader = new StreamReader(sourceFile))
                    {
                        deserialized = (T?)serializer.Deserialize(reader);
                    }
                }
                catch (Exception ex)
                {
                    // Catch everything, so we don't bomb out JF.
                    logger.LogError(ex, "Exception occurred migrating settings.");
                }

                xmlSerializer.SerializeToFile(settings, destFile);
                logger.LogDebug("Successfully migrated settings.");
            }
        }
    }
}
