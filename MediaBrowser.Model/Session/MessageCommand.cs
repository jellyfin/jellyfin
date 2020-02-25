#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Session
{
    public class MessageCommand
    {
        public string Header { get; set; }

        public string Text { get; set; }

        public long? TimeoutMs { get; set; }
    }
}
