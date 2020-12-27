using System;
using System.Linq;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ContainerProfile" />.
    /// </summary>
    public class ContainerProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerProfile"/> class.
        /// </summary>
        public ContainerProfile()
        {
            Conditions = Array.Empty<ProfileCondition>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerProfile"/> class.
        /// </summary>
        /// <param name="type">The <see cref="DlnaProfileType"/>.</param>
        /// <param name="conditions">Array of <see cref="ProfileCondition"/>.</param>
        public ContainerProfile(DlnaProfileType type, ProfileCondition[] conditions)
        {
            Type = type;
            Conditions = conditions;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerProfile"/> class.
        /// </summary>
        /// <param name="type">The <see cref="DlnaProfileType"/>.</param>
        /// <param name="container">The container name.</param>
        /// <param name="conditions">Array of <see cref="ProfileCondition"/>.</param>
        public ContainerProfile(DlnaProfileType type, string container, ProfileCondition[] conditions)
        {
            Type = type;
            Conditions = conditions;
            Container = container;
        }

        /// <summary>
        /// Gets or sets the dlna profile type.
        /// </summary>
        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }

        /// <summary>
        /// Gets or sets the conditions.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public ProfileCondition[] Conditions { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets or sets the container.
        /// </summary>
        [XmlAttribute("container")]
        public string? Container { get; set; }

        /// <summary>
        /// Splits a comma separated value.
        /// </summary>
        /// <param name="value">The value to split.</param>
        /// <returns>Array of value strings.</returns>
        public static string[] SplitValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<string>();
            }

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Checks to see if <paramref name="profileContainers"/> contains <paramref name="inputContainer"/>.
        /// </summary>
        /// <param name="profileContainers">The profile containers.</param>
        /// <param name="inputContainer">The input container.</param>
        /// <returns>True if <paramref name="inputContainer"/> is found.</returns>
        public static bool ContainsContainer(string? profileContainers, string inputContainer)
        {
            if (profileContainers == null)
            {
                return false;
            }

            var isNegativeList = false;
            if (profileContainers.StartsWith('-'))
            {
                isNegativeList = true;
                profileContainers = profileContainers[1..];
            }

            return ContainsContainer(SplitValue(profileContainers), isNegativeList, inputContainer);
        }

        /// <summary>
        /// Checks if the <paramref name="profileContainers"/> contains <paramref name="inputContainer"/>.
        /// </summary>
        /// <param name="profileContainers">The profile containers.</param>
        /// <param name="inputContainer">The input container.</param>
        /// <returns>True if <paramref name="inputContainer"/> is found.</returns>
        public static bool ContainsContainer(string[] profileContainers, string inputContainer)
        {
            return ContainsContainer(profileContainers, false, inputContainer);
        }

        /// <summary>
        /// Checks if the <paramref name="profileContainers"/> contains <paramref name="inputContainer"/>.
        /// </summary>
        /// <param name="profileContainers">The profile containers.</param>
        /// <param name="isNegativeList">True if this is a negative list.</param>
        /// <param name="inputContainer">The input container.</param>
        /// <returns>True if <paramref name="inputContainer"/> is found.</returns>
        public static bool ContainsContainer(string?[] profileContainers, bool isNegativeList, string inputContainer)
        {
            if (profileContainers == null || profileContainers.Length == 0)
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
                    if (profileContainers.Contains(container, StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the containers.
        /// </summary>
        /// <returns>String array of containers.</returns>
        public string[] GetContainers()
        {
            return SplitValue(Container);
        }

        /// <summary>
        /// Checks to see if this objects containers contains <paramref name="container"/>.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>True if <paramref name="container"/> is found.</returns>
        public bool ContainsContainer(string container)
        {
            var containers = GetContainers();

            return ContainsContainer(containers, container);
        }
    }
}
