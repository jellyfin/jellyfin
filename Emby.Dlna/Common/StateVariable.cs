#pragma warning disable CS1591
#pragma warning disable SA1600

using System;

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

        public string[] AllowedValues { get; set; }

        /// <inheritdoc />
        public override string ToString()
            => Name;
    }
}
