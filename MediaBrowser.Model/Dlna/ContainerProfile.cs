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

        public string[] GetContainers()
        {
            return SplitValue(Container);
        }

        private static readonly string[] EmptyStringArray = new string[] { };

        public static string[] SplitValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return EmptyStringArray;
            }

            return value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public bool ContainsContainer(string container)
        {
            var containers = GetContainers();

            return ContainsContainer(containers, container);
        }

        public static bool ContainsContainer(string profileContainers, string inputContainer)
        {
            return ContainsContainer(SplitValue(profileContainers), inputContainer);
        }

        public static bool ContainsContainer(string[] profileContainers, string inputContainer)
        {
            if (profileContainers.Length == 0)
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
