using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MediaBrowser.Common.Serialization
{
    public static class ProtobufSerializer
    {
        public static void SerializeToStream<T>(T obj, Stream stream)
        {
            ProtoBuf.Serializer.Serialize<T>(stream, obj);
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            return ProtoBuf.Serializer.Deserialize<T>(stream);
        }

        public static void SerializeToFile<T>(T obj, string file)
        {
            using (Stream stream = File.Open(file, FileMode.Create))
            {
                SerializeToStream<T>(obj, stream);
            }
        }

        public static T DeserializeFromFile<T>(string file)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return DeserializeFromStream<T>(stream);
            }
        }
    }
}
