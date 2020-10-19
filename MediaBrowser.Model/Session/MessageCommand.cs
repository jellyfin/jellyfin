#nullable disable
#pragma warning disable CS1591

namespace MediaBrowser.Model.Session
{
    public class MessageCommand
    {
        public string Header { get; set; }

        public string Text { get; set; }

        public long? TimeoutMs { get; set; }
    }
}
