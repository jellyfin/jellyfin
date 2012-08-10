
namespace MediaBrowser.Model.Entities
{
    public class Audio : BaseItem
    {
        public int BitRate { get; set; }
        public int Channels { get; set; }
        public int SampleRate { get; set; }
    }
}
