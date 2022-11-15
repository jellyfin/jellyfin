#nullable disable
#pragma warning disable CS1591

using System.ComponentModel.DataAnnotations;

namespace MediaBrowser.Model.Session
{
    public class MessageCommand
    {
        public string Header { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Text { get; set; }

        public long? TimeoutMs { get; set; }
    }
}
