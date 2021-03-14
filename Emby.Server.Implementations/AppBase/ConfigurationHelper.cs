#nullable enable

using System;
using System.IO;
using System.Linq;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Serialization;

namespace Emby.Server.Implementations.AppBase
{
    /// <summary>
    /// Class ConfigurationHelper.
    /// </summary>
    public static class ConfigurationHelper
    {
        /// <summary>
        /// Reads an xml configuration file from the file system
        /// It will immediately re-serialize and save if new serialization data is available due to property changes.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="path">The path.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <returns>System.Object.</returns>
        public static object GetXmlConfiguration(Type type, string path, IXmlSerializer xmlSerializer)
        {
            object configuration;

            byte[]? buffer = null;

            // Use try/catch to avoid the extra file system lookup using File.Exists
            try
            {
                buffer = File.ReadAllBytes(path);

                configuration = xmlSerializer.DeserializeFromBytes(type, buffer);
            }
            catch (Exception)
            {
                configuration = Activator.CreateInstance(type) ?? throw new ArgumentException($"Provided path ({type}) is not valid.", nameof(type));
            }

            using var stream = new MemoryStream(buffer?.Length ?? 0);
            xmlSerializer.SerializeToStream(configuration, stream);

            // Take the object we just got and serialize it back to bytes
            byte[] newBytes = stream.GetBuffer();
            int newBytesLen = (int)stream.Length;

            // If the file didn't exist before, or if something has changed, re-save
            if (buffer == null || !newBytes.AsSpan(0, newBytesLen).SequenceEqual(buffer))
            {
                var directory = Path.GetDirectoryName(path) ?? throw new ArgumentException($"Provided path ({path}) is not valid.", nameof(path));

                Directory.CreateDirectory(directory);
                // Save it after load in case we got new items
                // use FileShare.None as this bypasses dotnet bug dotnet/runtime#42790 .
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fs.Write(newBytes, 0, newBytesLen);
                }
            }

            return configuration;
        }
    }
}
