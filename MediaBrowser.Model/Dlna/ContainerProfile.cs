using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Extensions;
using System.Linq;

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

        private static readonly string[] EmptyStringArray = Array.Empty<string>();

        public static string[] SplitValue(string value)
        {
            if (string.IsNullOrEmpty(value))
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
            var isNegativeList = false;
            if (profileContainers != null && profileContainers.StartsWith("-"))
            {
                isNegativeList = true;
                profileContainers = profileContainers.Substring(1);
            }

            return ContainsContainer(SplitValue(profileContainers), isNegativeList, inputContainer);
        }

        public static bool ContainsContainer(string[] profileContainers, string inputContainer)
        {
            return ContainsContainer(profileContainers, false, inputContainer);
        }

        public static bool ContainsContainer(string[] profileContainers, bool isNegativeList, string inputContainer)
        {
            if (profileContainers.Length == 0)
            {
                return true;
            }

            if (isNegativeList)
            {
                var allInputContainers = SplitValue(inputContainer);

                foreach (var container in allInputContainers)
                {
                    if (profileContainers.Contains(container, StringComparer.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
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
}
