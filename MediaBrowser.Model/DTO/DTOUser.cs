using ProtoBuf;
using System;

namespace MediaBrowser.Model.DTO
{
    [ProtoContract]
    public class DtoUser
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public Guid Id { get; set; }

        [ProtoMember(3)]
        public bool HasImage { get; set; }

        [ProtoMember(4)]
        public bool HasPassword { get; set; }

        [ProtoMember(5)]
        public DateTime? LastLoginDate { get; set; }

        [ProtoMember(6)]
        public DateTime? LastActivityDate { get; set; }
    }
}
