
namespace MediaBrowser.Model.Connect
{
    public class PinCreationResult
    {
        public string Pin { get; set; }
        public string DeviceId { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsExpired { get; set; }
    }
}
