using System.Collections.Generic;

namespace Emby.Dlna.Common
{
    public class ServiceAction
    {
        public string Name { get; set; }

        public List<Argument> ArgumentList { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public ServiceAction()
        {
            ArgumentList = new List<Argument>();
        }
    }
}
