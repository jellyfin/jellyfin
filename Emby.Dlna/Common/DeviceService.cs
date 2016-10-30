
namespace Emby.Dlna.Common
{
    public class DeviceService
    {
        public string ServiceType { get; set; }

        public string ServiceId { get; set; }

        public string ScpdUrl { get; set; }

        public string ControlUrl { get; set; }

        public string EventSubUrl { get; set; }

        public override string ToString()
        {
            return string.Format("{0}", ServiceId);
        }
    }
}
