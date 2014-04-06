
namespace MediaBrowser.Dlna.PlayTo
{
    public class uBaseObject 
    {
        public string Id { get; set; }

        public string ParentId { get; set; }

        public string Title { get; set; }

        public string SecondText { get; set; }

        public string IconUrl { get; set; }

        public string MetaData { get; set; }

        public string Url { get; set; }

        public string[] ProtocolInfo { get; set; }
    }
}
