#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace Emby.Dlna.Common
{
    public class StateVariable
    {
        public StateVariable()
        {
            AllowedValues = Array.Empty<string>();
        }

        public string Name { get; set; }

        public string DataType { get; set; }

        public bool SendsEvents { get; set; }

        public IReadOnlyList<string> AllowedValues { get; set; }

        /// <inheritdoc />
        public override string ToString()
            => Name;
    }
}
