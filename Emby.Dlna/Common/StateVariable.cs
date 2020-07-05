#pragma warning disable CS1591
#pragma warning disable CA1819 // Properties should not return arrays

using System;

namespace Emby.Dlna.Common
{
    public class StateVariable
    {
        public StateVariable()
        {
            AllowedValues = Array.Empty<string>();
            DataType = string.Empty;
            Name = string.Empty;
        }

        public string Name { get; set; }

        public string DataType { get; set; }

        public bool SendsEvents { get; set; }

        public string[] AllowedValues { get; set; }

        /// <inheritdoc />
        public override string ToString()
            => Name;
    }
}
