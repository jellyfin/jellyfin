using ProtoBuf;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Since it can be slow to collect this data, this class helps provide a way to calculate them all at once.
    /// </summary>
    [ProtoContract]
    public class ItemSpecialCounts
    {
        [ProtoMember(1)]
        public int RecentlyAddedItemCount { get; set; }

        [ProtoMember(2)]
        public int RecentlyAddedUnPlayedItemCount { get; set; }

        [ProtoMember(3)]
        public int InProgressItemCount { get; set; }

        [ProtoMember(4)]
        public decimal PlayedPercentage { get; set; }
    }

    [ProtoContract]
    public class AudioStream
    {
        [ProtoMember(1)]
        public string Codec { get; set; }

        [ProtoMember(2)]
        public string Language { get; set; }

        [ProtoMember(3)]
        public int BitRate { get; set; }

        [ProtoMember(4)]
        public int Channels { get; set; }

        [ProtoMember(5)]
        public int SampleRate { get; set; }

        [ProtoMember(6)]
        public bool IsDefault { get; set; }
    }

    [ProtoContract]
    public class SubtitleStream
    {
        [ProtoMember(1)]
        public string Language { get; set; }

        [ProtoMember(2)]
        public bool IsDefault { get; set; }

        [ProtoMember(3)]
        public bool IsForced { get; set; }
    }

    public enum VideoType
    {
        VideoFile,
        Iso,
        DVD,
        BluRay
    }
}
