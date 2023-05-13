using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Emby.Dlna.Common
{
    /// <summary>
    /// Defines the <see cref="ServiceAction" />.
    /// </summary>
    public class ServiceAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceAction"/> class.
        /// </summary>
        public ServiceAction()
        {
        }

        /// <summary>
        /// Gets or sets the name of the action.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the ArgumentList.
        /// </summary>
        public Collection<Argument> ArgumentList { get; } = new Collection<Argument>();

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
