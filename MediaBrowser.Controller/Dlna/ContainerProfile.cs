using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace MediaBrowser.Controller.Dlna
{
    public class ContainerProfile
    {
        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }
        public ProfileCondition[] Conditions { get; set; }

        [XmlAttribute("container")]
        public string Container { get; set; }

        public ContainerProfile()
        {
            Conditions = new ProfileCondition[] { };
        }

        public List<string> GetContainers()
        {
            return (Container ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        }
    }
}
