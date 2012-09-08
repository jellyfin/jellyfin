using ProtoBuf;

namespace MediaBrowser.Model.Authentication
{
    [ProtoContract]
    public class AuthenticationResult
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
    }
}
