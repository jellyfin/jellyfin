using System;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{  
    public class Argument
    {
        public string Name { get;  set; }

        public string Direction { get;  set; }

        public string RelatedStateVariable { get;  set; }

        public static Argument FromXml(XElement container)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            return new Argument
            {
                Name = container.GetValue(uPnpNamespaces.svc + "name"),
                Direction = container.GetValue(uPnpNamespaces.svc + "direction"),
                RelatedStateVariable = container.GetValue(uPnpNamespaces.svc + "relatedStateVariable")                
            };
        }
    }
}
