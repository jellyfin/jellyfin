using System.Xml.Serialization;
using MediaBrowser.Controller.Dlna;

namespace MediaBrowser.Dlna.Profiles
{
    [XmlRoot("Profile")]
    public class Foobar2000Profile : DefaultProfile
    {
        public Foobar2000Profile()
        {
            Name = "foobar2000";

            Identification = new DeviceIdentification
            {
                FriendlyName = @"foobar",

                Headers = new[]
               {
                   new HttpHeaderInfo
                   {
                       Name = "User-Agent",
                       Value = "foobar",
                       Match = HeaderMatchType.Substring
                   }
               }
            };
        }
    }
}
