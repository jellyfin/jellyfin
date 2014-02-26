using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class StateVariable
    {
        public string Name { get; set; }

        public string DataType { get; set; }

        private List<string> _allowedValues = new List<string>();
        public List<string> AllowedValues
        {
            get
            {
                return _allowedValues;
            }
            set
            {
                _allowedValues = value;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        public static StateVariable FromXml(XElement container)
        {
            var allowedValues = new List<string>();
            var element = container.Descendants(uPnpNamespaces.svc + "allowedValueList")
                .FirstOrDefault();
            
            if (element != null)
            {
                var values = element.Descendants(uPnpNamespaces.svc + "allowedValue");

                allowedValues.AddRange(values.Select(child => child.Value));
            }

            return new StateVariable
            {
                Name = container.GetValue(uPnpNamespaces.svc + "name"),
                DataType = container.GetValue(uPnpNamespaces.svc + "dataType"),
                AllowedValues = allowedValues
            };
        }
    }
}
