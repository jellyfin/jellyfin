using ProtoBuf;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Serves as a common base class for the Server and UI application Configurations
    /// ProtoInclude tells Protobuf about subclasses,
    /// The number 50 can be any number, so long as it doesn't clash with any of the ProtoMember numbers either here or in subclasses.
    /// </summary>
    [ProtoContract, ProtoInclude(50, typeof(ServerConfiguration))]
    public class BaseApplicationConfiguration
    {
        [ProtoMember(1)]
        public bool EnableDebugLevelLogging { get; set; }

        [ProtoMember(2)]
        public int HttpServerPortNumber { get; set; }

        public BaseApplicationConfiguration()
        {
            HttpServerPortNumber = 8096;
        }
    }
}
