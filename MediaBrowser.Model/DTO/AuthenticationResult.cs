using System;
using ProtoBuf;

namespace MediaBrowser.Model.DTO
{
    [ProtoContract]
    public class AuthenticationResult
    {
        [ProtoMember(1)]
        public bool Success { get; set; }
    }
}
