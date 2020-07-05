#pragma warning disable CS1591
#pragma warning disable CA2227 // Collection properties should be read only

using System.Collections.Generic;

namespace Emby.Dlna.Common
{
    public class ServiceAction
    {
        public ServiceAction()
        {
            ArgumentList = new List<Argument>();
            Name = string.Empty;
        }

        public string Name { get; set; }

        public List<Argument> ArgumentList { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }
    }
}
