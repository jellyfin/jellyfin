namespace MediaBrowser.Model.Dlna
{
    public class SubtitleStreamInfo
    {
        public string Url { get; set; }
        public string Language { get; set; }
        public string Name { get; set; }
        public bool IsForced { get; set; }
        public string Format { get; set; }
    }
}