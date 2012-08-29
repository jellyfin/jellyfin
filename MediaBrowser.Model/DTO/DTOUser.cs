using System;
using ProtoBuf;

namespace MediaBrowser.Model.DTO
{
    [ProtoContract]
    public class DTOUser
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public Guid Id { get; set; }

        [ProtoMember(3)]
        public bool HasImage { get; set; }
    }
}
