
namespace MediaBrowser.Model.Connect
{
    public class PinStatusResult
    {
        public string Pin { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsExpired { get; set; }
    }
}
