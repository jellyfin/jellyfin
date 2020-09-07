#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dlna
{
    public class StateVariable
    {
        public string Name { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;

        public bool SendsEvents { get; set; }

        public IReadOnlyList<string> AllowedValues { get; set; } = Array.Empty<string>();

        /// <inheritdoc />
        public override string ToString()
            => Name;
    }
}
