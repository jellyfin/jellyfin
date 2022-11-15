#pragma warning disable CS1591

using System.Xml.Linq;

namespace Emby.Dlna.PlayTo
{
    public static class UPnpNamespaces
    {
        public static XNamespace Dc { get; } = "http://purl.org/dc/elements/1.1/";

        public static XNamespace Ns { get; } = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";

        public static XNamespace Svc { get; } = "urn:schemas-upnp-org:service-1-0";

        public static XNamespace Ud { get; } = "urn:schemas-upnp-org:device-1-0";

        public static XNamespace UPnp { get; } = "urn:schemas-upnp-org:metadata-1-0/upnp/";

        public static XNamespace RenderingControl { get; } = "urn:schemas-upnp-org:service:RenderingControl:1";

        public static XNamespace AvTransport { get; } = "urn:schemas-upnp-org:service:AVTransport:1";

        public static XNamespace ContentDirectory { get; } = "urn:schemas-upnp-org:service:ContentDirectory:1";

        public static XName Containers { get; } = Ns + "container";

        public static XName Items { get; } = Ns + "item";

        public static XName Title { get; } = Dc + "title";

        public static XName Creator { get; } = Dc + "creator";

        public static XName Artist { get; } = UPnp + "artist";

        public static XName Id { get; } = "id";

        public static XName ParentId { get; } = "parentID";

        public static XName Class { get; } = UPnp + "class";

        public static XName Artwork { get; } = UPnp + "albumArtURI";

        public static XName Description { get; } = Dc + "description";

        public static XName LongDescription { get; } = UPnp + "longDescription";

        public static XName Album { get; } = UPnp + "album";

        public static XName Author { get; } = UPnp + "author";

        public static XName Director { get; } = UPnp + "director";

        public static XName PlayCount { get; } = UPnp + "playbackCount";

        public static XName Tracknumber { get; } = UPnp + "originalTrackNumber";

        public static XName Res { get; } = Ns + "res";

        public static XName Duration { get; } = "duration";

        public static XName ProtocolInfo { get; } = "protocolInfo";

        public static XName ServiceStateTable { get; } = Svc + "serviceStateTable";

        public static XName StateVariable { get; } = Svc + "stateVariable";
    }
}
