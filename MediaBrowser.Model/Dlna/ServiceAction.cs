#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Model.Dlna
{
    public class ServiceAction
    {
        public ServiceAction()
        {
            ArgumentList = new List<Argument>();
        }

        public string Name { get; set; } = string.Empty;

        public List<Argument> ArgumentList { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }
}
