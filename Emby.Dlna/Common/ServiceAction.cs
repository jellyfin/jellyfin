#pragma warning disable CS1591

using System.Collections.Generic;

namespace Emby.Dlna.Common
{
    public class ServiceAction
    {
        public ServiceAction()
        {
            ArgumentList = new List<Argument>();
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
