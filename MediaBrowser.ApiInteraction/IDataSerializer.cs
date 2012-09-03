using System;
using System.IO;

namespace MediaBrowser.ApiInteraction
{
    /// <summary>
    /// Since ServiceStack Json is not portable, we need to abstract required json functions into an interface
    /// </summary>
    public interface IDataSerializer
    {
        T DeserializeJsonFromStream<T>(Stream stream);
        T DeserializeJsvFromStream<T>(Stream stream);
        T DeserializeProtobufFromStream<T>(Stream stream);

        object DeserializeJsonFromStream(Stream stream, Type type);
        object DeserializeJsvFromStream(Stream stream, Type type);
        object DeserializeProtobufFromStream(Stream stream, Type type);

        bool CanDeserializeJsv { get; }
        bool CanDeserializeProtobuf { get; }
    }
}
