using System;
using System.Collections.Generic;

namespace Emby.Dlna.Common
{
    /// <summary>
    /// Defines the <see cref="StateVariable" />.
    /// </summary>
    public class StateVariable
    {
        /// <summary>
        /// Gets or sets the name of the state variable.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data type of the state variable.
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether it sends events.
        /// </summary>
        public bool SendsEvents { get; set; }

        /// <summary>
        /// Gets or sets the allowed values range.
        /// </summary>
        public IReadOnlyList<string> AllowedValues { get; set; } = Array.Empty<string>();

        /// <inheritdoc />
        public override string ToString() => Name;
    }
}
