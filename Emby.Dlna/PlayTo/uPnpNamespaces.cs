#pragma warning disable CS1591

using System.Xml.Linq;

namespace Emby.Dlna.PlayTo
{
    public class uPnpNamespaces
    {
        public static XNamespace dc = "http://purl.org/dc/elements/1.1/";
        public static XNamespace ns = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
        public static XNamespace svc = "urn:schemas-upnp-org:service-1-0";
        public static XNamespace ud = "urn:schemas-upnp-org:device-1-0";
        public static XNamespace upnp = "urn:schemas-upnp-org:metadata-1-0/upnp/";
        public static XNamespace RenderingControl = "urn:schemas-upnp-org:service:RenderingControl:1";
        public static XNamespace AvTransport = "urn:schemas-upnp-org:service:AVTransport:1";
        public static XNamespace ContentDirectory = "urn:schemas-upnp-org:service:ContentDirectory:1";

        public static XName containers = ns + "container";
        public static XName items = ns + "item";
        public static XName title = dc + "title";
        public static XName creator = dc + "creator";
        public static XName artist = upnp + "artist";
        public static XName Id = "id";
        public static XName ParentId = "parentID";
        public static XName uClass = upnp + "class";
        public static XName Artwork = upnp + "albumArtURI";
        public static XName Description = dc + "description";
        public static XName LongDescription = upnp + "longDescription";
        public static XName Album = upnp + "album";
        public static XName Author = upnp + "author";
        public static XName Director = upnp + "director";
        public static XName PlayCount = upnp + "playbackCount";
        public static XName Tracknumber = upnp + "originalTrackNumber";
        public static XName Res = ns + "res";
        public static XName Duration = "duration";
        public static XName ProtocolInfo = "protocolInfo";

        public static XName ServiceStateTable = svc + "serviceStateTable";
        public static XName StateVariable = svc + "stateVariable";
    }
}
