using MediaBrowser.Model.Serialization;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Common.Implementations.Serialization
{
    /// <summary>
    /// Creates a compiled protobuf serializer based on a set of assemblies
    /// </summary>
    public class ProtobufSerializer : IProtobufSerializer
    {
        /// <summary>
        /// Gets or sets the type model.
        /// </summary>
        /// <value>The type model.</value>
        private TypeModel TypeModel { get; set; }

        /// <summary>
        /// Serializes to stream.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="stream">The stream.</param>
        /// <exception cref="System.ArgumentNullException">obj</exception>
        public void SerializeToStream(object obj, Stream stream)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            TypeModel.Serialize(stream, obj);
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public object DeserializeFromStream(Stream stream, Type type)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            
            return TypeModel.Deserialize(stream, null, type);
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream">The stream.</param>
        /// <returns>``0.</returns>
        public T DeserializeFromStream<T>(Stream stream)
            where T : class
        {
            return DeserializeFromStream(stream, typeof(T)) as T;
        }

        /// <summary>
        /// Serializes to file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        /// <param name="file">The file.</param>
        /// <exception cref="System.ArgumentNullException">file</exception>
        public void SerializeToFile<T>(T obj, string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }
            
            using (Stream stream = File.Open(file, FileMode.Create))
            {
                SerializeToStream(obj, stream);
            }
        }

        /// <summary>
        /// Deserializes from file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file">The file.</param>
        /// <returns>``0.</returns>
        /// <exception cref="System.ArgumentNullException">file</exception>
        public T DeserializeFromFile<T>(string file)
            where T : class
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }
            
            using (Stream stream = File.OpenRead(file))
            {
                return DeserializeFromStream<T>(stream);
            }
        }

        /// <summary>
        /// Serializes to bytes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Byte[][].</returns>
        /// <exception cref="System.ArgumentNullException">obj</exception>
        public byte[] SerializeToBytes<T>(T obj)
            where T : class
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            
            using (var stream = new MemoryStream())
            {
                SerializeToStream(obj, stream);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Creates the specified assemblies.
        /// </summary>
        /// <returns>DynamicProtobufSerializer.</returns>
        /// <exception cref="System.ArgumentNullException">assemblies</exception>
        public static ProtobufSerializer Create(IEnumerable<Type> types)
        {
            if (types == null)
            {
                throw new ArgumentNullException("types");
            }
            
            var model = TypeModel.Create();
            var attributeType = typeof(ProtoContractAttribute);

            // Find all ProtoContracts in the current assembly
            foreach (var type in types.Where(t => Attribute.IsDefined(t, attributeType)))
            {
                model.Add(type, true);
            }

            return new ProtobufSerializer { TypeModel = model.Compile() };
        }
    }
}
