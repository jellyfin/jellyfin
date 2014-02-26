using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class uService
    {
        public string ServiceType { get; set; }

        public string ServiceId { get; set; }

        public string SCPDURL { get; set; }

        public string ControlURL { get; set; }

        public string EventSubURL { get; set; }

        public uService(string serviceType, string serviceId, string scpdUrl, string controlUrl, string eventSubUrl)
        {
            ServiceType = serviceType;
            ServiceId = serviceId;
            SCPDURL = scpdUrl;
            ControlURL = controlUrl;
            EventSubURL = eventSubUrl;
        }

        public static uService Create(XElement element)
        {
            var type = element.GetDescendantValue(uPnpNamespaces.ud.GetName("serviceType"));
            var id = element.GetDescendantValue(uPnpNamespaces.ud.GetName("serviceId"));
            var scpdUrl = element.GetDescendantValue(uPnpNamespaces.ud.GetName("SCPDURL"));
            var controlURL = element.GetDescendantValue(uPnpNamespaces.ud.GetName("controlURL"));
            var eventSubURL = element.GetDescendantValue(uPnpNamespaces.ud.GetName("eventSubURL"));

            return new uService(type, id, scpdUrl, controlURL, eventSubURL);
        }

        public override string ToString()
        {
            return string.Format("{0}", ServiceId);
        }
    }
}
