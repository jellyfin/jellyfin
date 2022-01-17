#pragma warning disable CS1591

using System;
using System.Xml.Serialization;
using Jellyfin.Extensions;

namespace MediaBrowser.Model.Dlna
{
    public class ContainerProfile
    {
        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }

        public ProfileCondition[]? Conditions { get; set; } = Array.Empty<ProfileCondition>();

        [XmlAttribute("container")]
        public string Container { get; set; } = string.Empty;

        public static string[] SplitValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<string>();
            }

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        public bool ContainsContainer(string? container)
        {
            var containers = SplitValue(Container);

            return ContainsContainer(containers, container);
        }

        public static bool ContainsContainer(string? profileContainers, string? inputContainer)
        {
            var isNegativeList = false;
            if (profileContainers != null && profileContainers.StartsWith('-'))
            {
                isNegativeList = true;
                profileContainers = profileContainers.Substring(1);
            }

            return ContainsContainer(SplitValue(profileContainers), isNegativeList, inputContainer);
        }

        public static bool ContainsContainer(string[]? profileContainers, string? inputContainer)
        {
            return ContainsContainer(profileContainers, false, inputContainer);
        }

        public static bool ContainsContainer(string[]? profileContainers, bool isNegativeList, string? inputContainer)
        {
            if (profileContainers == null || profileContainers.Length == 0)
            {
                // Empty profiles always support all containers/codecs
                return true;
            }

            var allInputContainers = SplitValue(inputContainer);

            foreach (var container in allInputContainers)
            {
                if (profileContainers.Contains(container, StringComparison.OrdinalIgnoreCase))
                {
                    return !isNegativeList;
                }
            }

            return isNegativeList;
        }
    }
}
