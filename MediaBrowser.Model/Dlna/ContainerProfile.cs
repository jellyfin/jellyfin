using System;
using System.Xml.Serialization;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ContainerProfile"/>.
    /// </summary>
    public class ContainerProfile
    {
        /// <summary>
        /// Gets or sets the <see cref="DlnaProfileType"/> which this container must meet.
        /// </summary>
        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }

        /// <summary>
        /// Gets or sets the list of <see cref="ProfileCondition"/> which this container will be applied to.
        /// </summary>
        public ProfileCondition[]? Conditions { get; set; }

        /// <summary>
        /// Gets or sets the container(s) which this container must meet.
        /// </summary>
        [XmlAttribute("container")]
        public string? Container { get; set; }

        /// <summary>
        /// Returns true if an item in <paramref name="container"/> appears in the <see cref="Container"/> property.
        /// </summary>
        /// <param name="container">The item to match.</param>
        /// <returns>The result of the operation.</returns>
        public bool ContainsContainer(ReadOnlySpan<char> container)
        {
            return Container.ContainsContainer(container);
        }
    }
}
