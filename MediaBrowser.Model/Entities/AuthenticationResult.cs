using ProtoBuf;

namespace MediaBrowser.Model.Entities
{
    [ProtoContract]
    public class AuthenticationResult
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
    }
}
