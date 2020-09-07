#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Model.Dlna
{
    public class ServiceAction
    {
        public string Name { get; set; } = string.Empty;

        public List<Argument> ArgumentList { get; } = new List<Argument>();

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }
}
