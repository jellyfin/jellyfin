using System.Collections.Generic;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class ServiceAction
    {
        public string Name { get; set; }

        public List<Argument> ArgumentList { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public static ServiceAction FromXml(XElement container)
        {
            var argumentList = new List<Argument>();

            foreach (var arg in container.Descendants(uPnpNamespaces.svc + "argument"))
            {
                argumentList.Add(Argument.FromXml(arg));
            }
            
            return new ServiceAction
            {
                Name = container.GetValue(uPnpNamespaces.svc + "name"),

                ArgumentList = argumentList
            };
        }
    }
}
