using System;
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
            return SplitValue(Container);
        }

        public static List<string> SplitValue(string value)
        {
            List<string> list = new List<string>();
            foreach (string i in (value ?? string.Empty).Split(','))
            {
                if (!string.IsNullOrWhiteSpace(i)) list.Add(i);
            }
            return list;
        }

        public bool ContainsContainer(string container)
        {
            List<string> containers = GetContainers();

            return ContainsContainer(containers, container);
        }

        public static bool ContainsContainer(string profileContainers, string inputContainer)
        {
            return ContainsContainer(SplitValue(profileContainers), inputContainer);
        }

        public static bool ContainsContainer(List<string> profileContainers, string inputContainer)
        {
            if (profileContainers.Count == 0)
            {
                return true;
            }

            var allInputContainers = SplitValue(inputContainer);

            foreach (var container in allInputContainers)
            {
                if (ListHelper.ContainsIgnoreCase(profileContainers, container))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
