using System.Collections.Generic;

namespace Emby.Dlna.Common
{
    public class StateVariable
    {
        public string Name { get; set; }

        public string DataType { get; set; }

        public bool SendsEvents { get; set; }

        public List<string> AllowedValues { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public StateVariable()
        {
            AllowedValues = new List<string>();
        }
    }
}
