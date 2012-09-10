using ProtoBuf;

namespace MediaBrowser.Model.DTO
{
    [ProtoContract]
    public class MovieInfo
    {
        [ProtoMember(1)]
        public int SpecialFeatureCount { get; set; }
    }
}
