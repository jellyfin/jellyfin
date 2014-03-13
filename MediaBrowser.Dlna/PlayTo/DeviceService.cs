
namespace MediaBrowser.Dlna.PlayTo
{
    public class DeviceService
    {
        public string ServiceType { get; set; }

        public string ServiceId { get; set; }

        public string ScpdUrl { get; set; }

        public string ControlUrl { get; set; }

        public string EventSubUrl { get; set; }

        public DeviceService(string serviceType, string serviceId, string scpdUrl, string controlUrl, string eventSubUrl)
        {
            ServiceType = serviceType;
            ServiceId = serviceId;
            ScpdUrl = scpdUrl;
            ControlUrl = controlUrl;
            EventSubUrl = eventSubUrl;
        }

        public override string ToString()
        {
            return string.Format("{0}", ServiceId);
        }
    }
}
