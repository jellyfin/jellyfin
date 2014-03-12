
using System;
namespace MediaBrowser.Model.Session
{
    public class MessageCommand
    {       
        public Guid UserId { get; set; }

        public string Header { get; set; }
        
        public string Text { get; set; }

        public long? TimeoutMs { get; set; }
    }
}
