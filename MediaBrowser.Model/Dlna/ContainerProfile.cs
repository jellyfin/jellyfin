using System.Collections.Generic;
using System.Xml.Serialization;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Model.Dlna
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
            List<string> list = new List<string>();
            foreach (string i in (Container ?? string.Empty).Split(','))
            {
                if (!string.IsNullOrEmpty(i)) list.Add(i);
            }
            return list;
        }

        public bool ContainsContainer(string container)
        {
            List<string> containers = GetContainers();

            return containers.Count == 0 || ListHelper.ContainsIgnoreCase(containers, container ?? string.Empty);
        }
    }
}
