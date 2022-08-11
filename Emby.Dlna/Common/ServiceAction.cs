using System.Collections.Generic;

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
            ArgumentList = new List<Argument>();
        }

        /// <summary>
        /// Gets or sets the name of the action.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the ArgumentList.
        /// </summary>
        public List<Argument> ArgumentList { get; }

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
