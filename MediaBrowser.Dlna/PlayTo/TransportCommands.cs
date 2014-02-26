using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class TransportCommands
    {
        List<StateVariable> _stateVariables = new List<StateVariable>();
        public List<StateVariable> StateVariables
        {
            get
            {
                return _stateVariables;
            }
            set
            {
                _stateVariables = value;
            }
        }

        List<ServiceAction> _serviceActions = new List<ServiceAction>();
        public List<ServiceAction> ServiceActions
        {
            get
            {
                return _serviceActions;
            }
            set
            {
                _serviceActions = value;
            }
        }

        public static TransportCommands Create(XDocument document)
        {
            var command = new TransportCommands();

            var actionList = document.Descendants(uPnpNamespaces.svc + "actionList");

            foreach (var container in actionList.Descendants(uPnpNamespaces.svc + "action"))
            {
                command.ServiceActions.Add(ServiceAction.FromXml(container));
            }

            var stateValues = document.Descendants(uPnpNamespaces.ServiceStateTable).FirstOrDefault();

            if (stateValues != null)
            {
                foreach (var container in stateValues.Elements(uPnpNamespaces.svc + "stateVariable"))
                {
                    command.StateVariables.Add(StateVariable.FromXml(container));
                }
            }

            return command;
        }

        public string BuildPost(ServiceAction action, string xmlNamespace)
        {
            var stateString = string.Empty;

            foreach (var arg in action.ArgumentList)
            {
                if (arg.Direction == "out")
                    continue;

                if (arg.Name == "InstanceID")
                    stateString += BuildArgumentXml(arg, "0");
                else
                    stateString += BuildArgumentXml(arg, null);
            }

            return string.Format(CommandBase, action.Name, xmlNamespace, stateString);
        }

        public string BuildPost(ServiceAction action, string xmlNamesapce, object value, string commandParameter = "")
        {
            var stateString = string.Empty;

            foreach (var arg in action.ArgumentList)
            {
                if (arg.Direction == "out")
                    continue;
                if (arg.Name == "InstanceID")
                    stateString += BuildArgumentXml(arg, "0");
                else
                    stateString += BuildArgumentXml(arg, value.ToString(), commandParameter);
            }

            return string.Format(CommandBase, action.Name, xmlNamesapce, stateString);
        }

        public string BuildSearchPost(ServiceAction action, string xmlNamesapce, object value, string commandParameter = "")
        {
            var stateString = string.Empty;

            foreach (var arg in action.ArgumentList)
            {
                if (arg.Direction == "out")
                    continue;

                if (arg.Name == "ObjectID")
                    stateString += BuildArgumentXml(arg, value.ToString());
                else if (arg.Name == "Filter")
                    stateString += BuildArgumentXml(arg, "*");
                else if (arg.Name == "StartingIndex")
                    stateString += BuildArgumentXml(arg, "0");
                else if (arg.Name == "RequestedCount")
                    stateString += BuildArgumentXml(arg, "200");
                else if (arg.Name == "BrowseFlag")
                    stateString += BuildArgumentXml(arg, null, "BrowseDirectChildren");
                else if (arg.Name == "SortCriteria")
                    stateString += BuildArgumentXml(arg, "");
                else
                    stateString += BuildArgumentXml(arg, value.ToString(), commandParameter);
            }

            return string.Format(CommandBase, action.Name, xmlNamesapce, stateString);
        }

        public string BuildPost(ServiceAction action, string xmlNamesapce, object value, Dictionary<string, string> dictionary)
        {
            var stateString = string.Empty;

            foreach (var arg in action.ArgumentList)
            {
                if (arg.Name == "InstanceID")
                    stateString += BuildArgumentXml(arg, "0");
                else if (dictionary.ContainsKey(arg.Name))
                    stateString += BuildArgumentXml(arg, dictionary[arg.Name]);
                else
                    stateString += BuildArgumentXml(arg, value.ToString());
            }

            return string.Format(CommandBase, action.Name, xmlNamesapce, stateString);
        }

        private string BuildArgumentXml(Argument argument, string value, string commandParameter = "")
        {
            var state = StateVariables.FirstOrDefault(a => a.Name == argument.RelatedStateVariable);

            if (state != null)
            {
                var sendValue = (state.AllowedValues.FirstOrDefault(a => a == commandParameter) ??
                                 state.AllowedValues.FirstOrDefault()) ?? 
                                 value;

                return string.Format("<{0} xmlns:dt=\"urn:schemas-microsoft-com:datatypes\" dt:dt=\"{1}\">{2}</{0}>", argument.Name, state.DataType ?? "string", sendValue);
            }

            return string.Format("<{0}>{1}</{0}>", argument.Name, value);
        }

        private const string CommandBase = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" + "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" SOAP-ENV:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" + "<SOAP-ENV:Body>" + "<m:{0} xmlns:m=\"{1}\">" + "{2}" + "</m:{0}>" + "</SOAP-ENV:Body></SOAP-ENV:Envelope>";
    }
}
