using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using MediaBrowser.Controller.Serialization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;

namespace Emby.Server.Implementations.Serialization
{
    /// <summary>
    /// Provides a wrapper around third party xml serialization.
    /// </summary>
    public class MyXmlSerializer : IXmlSerializer
    {
        // Need to cache these
        // http://dotnetcodebox.blogspot.com/2013/01/xmlserializer-class-may-result-in.html
        private static readonly ConcurrentDictionary<string, XmlSerializer> _serializers =
            new ConcurrentDictionary<string, XmlSerializer>();

        private static XmlSerializer GetSerializer(Type type)
        {
            var serializer = _serializers.GetOrAdd(type.FullName, _ => new XmlSerializer(type));
            serializer.UnknownElement += SerializerOnUnknownElement;
            return serializer;
        }

        /// <summary>
        /// Serializes to writer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="writer">The writer.</param>
        private void SerializeToWriter(object obj, XmlWriter writer)
        {
            var netSerializer = GetSerializer(obj.GetType());
            netSerializer.Serialize(writer, obj);
        }

        /// <inheritdoc />
        public object DeserializeFromStream(Type type, Stream stream)
        {
            using (var reader = XmlReader.Create(stream))
            {
                var netSerializer = GetSerializer(type);
                return netSerializer.Deserialize(reader);
            }
        }

        /// <inheritdoc />
        public void SerializeToStream(object obj, Stream stream)
        {
            using (var writer = new StreamWriter(stream, null, IODefaults.StreamWriterBufferSize, true))
            using (var textWriter = new XmlTextWriter(writer))
            {
                textWriter.Formatting = Formatting.Indented;
                SerializeToWriter(obj, textWriter);
            }
        }

        /// <inheritdoc />
        public void SerializeToFile(object obj, string file)
        {
            using (var stream = new FileStream(file, FileMode.Create))
            {
                SerializeToStream(obj, stream);
            }
        }

        /// <inheritdoc />
        public object DeserializeFromFile(Type type, string file)
        {
            using (var stream = File.OpenRead(file))
            {
                return DeserializeFromStream(type, stream);
            }
        }

        /// <inheritdoc />
        public object DeserializeFromBytes(Type type, byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer, 0, buffer.Length, false, true))
            {
                return DeserializeFromStream(type, stream);
            }
        }

        private static void SerializerOnUnknownElement(object sender, XmlElementEventArgs e)
        {
            var member = XmlSynonymsAttribute.GetMember(e.ObjectBeingDeserialized, e.Element.Name);
            Type memberType;

            if (member != null && member is FieldInfo info)
            {
                memberType = info.FieldType;
            }
            else if (member != null && member is PropertyInfo propertyInfo)
            {
                memberType = propertyInfo.PropertyType;
            }
            else
            {
                return;
            }

            object value;
            XmlSerializer serializer = new XmlSerializer(memberType, new XmlRootAttribute(e.Element.Name));
            using StringReader stringReader = new StringReader(e.Element.OuterXml);
            using XmlReader reader = XmlReader.Create(stringReader);
            value = serializer.Deserialize(reader);

            if (member is FieldInfo)
            {
                ((FieldInfo)member).SetValue(e.ObjectBeingDeserialized, value);
            }
            else if (member is PropertyInfo)
            {
                ((PropertyInfo)member).SetValue(e.ObjectBeingDeserialized, value);
            }
        }
    }
}
