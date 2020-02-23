#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Emby.Dlna.Common;
using Emby.Dlna.Ssdp;

namespace Emby.Dlna.PlayTo
{
    public class TransportCommands
    {
        private List<StateVariable> _stateVariables = new List<StateVariable>();
        public List<StateVariable> StateVariables
        {
            get => _stateVariables;
            set => _stateVariables = value;
        }

        private List<ServiceAction> _serviceActions = new List<ServiceAction>();
        public List<ServiceAction> ServiceActions
        {
            get => _serviceActions;
            set => _serviceActions = value;
        }

        public static TransportCommands Create(XDocument document)
        {
            var command = new TransportCommands();

            var actionList = document.Descendants(uPnpNamespaces.svc + "actionList");

            foreach (var container in actionList.Descendants(uPnpNamespaces.svc + "action"))
            {
                command.ServiceActions.Add(ServiceActionFromXml(container));
            }

            var stateValues = document.Descendants(uPnpNamespaces.ServiceStateTable).FirstOrDefault();

            if (stateValues != null)
            {
                foreach (var container in stateValues.Elements(uPnpNamespaces.svc + "stateVariable"))
                {
                    command.StateVariables.Add(FromXml(container));
                }
            }

            return command;
        }

        private static ServiceAction ServiceActionFromXml(XElement container)
        {
            var argumentList = new List<Argument>();

            foreach (var arg in container.Descendants(uPnpNamespaces.svc + "argument"))
            {
                argumentList.Add(ArgumentFromXml(arg));
            }

            return new ServiceAction
            {
                Name = container.GetValue(uPnpNamespaces.svc + "name"),

                ArgumentList = argumentList
            };
        }

        private static Argument ArgumentFromXml(XElement container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            return new Argument
            {
                Name = container.GetValue(uPnpNamespaces.svc + "name"),
                Direction = container.GetValue(uPnpNamespaces.svc + "direction"),
                RelatedStateVariable = container.GetValue(uPnpNamespaces.svc + "relatedStateVariable")
            };
        }

        private static StateVariable FromXml(XElement container)
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
                AllowedValues = allowedValues.ToArray()
            };
        }

        public string BuildPost(ServiceAction action, string xmlNamespace)
        {
            var stateString = string.Empty;

            foreach (var arg in action.ArgumentList)
            {
                if (arg.Direction == "out")
                {
                    continue;
                }

                if (arg.Name == "InstanceID")
                {
                    stateString += BuildArgumentXml(arg, "0");
                }
                else
                {
                    stateString += BuildArgumentXml(arg, null);
                }
            }

            return string.Format(CommandBase, action.Name, xmlNamespace, stateString);
        }

        public string BuildPost(ServiceAction action, string xmlNamesapce, object value, string commandParameter = "")
        {
            var stateString = string.Empty;

            foreach (var arg in action.ArgumentList)
            {
                if (arg.Direction == "out")
                {
                    continue;
                }

                if (arg.Name == "InstanceID")
                {
                    stateString += BuildArgumentXml(arg, "0");
                }
                else
                {
                    stateString += BuildArgumentXml(arg, value.ToString(), commandParameter);
                }
            }

            return string.Format(CommandBase, action.Name, xmlNamesapce, stateString);
        }

        public string BuildPost(ServiceAction action, string xmlNamesapce, object value, Dictionary<string, string> dictionary)
        {
            var stateString = string.Empty;

            foreach (var arg in action.ArgumentList)
            {
                if (arg.Name == "InstanceID")
                {
                    stateString += BuildArgumentXml(arg, "0");
                }
                else if (dictionary.ContainsKey(arg.Name))
                {
                    stateString += BuildArgumentXml(arg, dictionary[arg.Name]);
                }
                else
                {
                    stateString += BuildArgumentXml(arg, value.ToString());
                }
            }

            return string.Format(CommandBase, action.Name, xmlNamesapce, stateString);
        }

        private string BuildArgumentXml(Argument argument, string value, string commandParameter = "")
        {
            var state = StateVariables.FirstOrDefault(a => string.Equals(a.Name, argument.RelatedStateVariable, StringComparison.OrdinalIgnoreCase));

            if (state != null)
            {
                var sendValue = state.AllowedValues.FirstOrDefault(a => string.Equals(a, commandParameter, StringComparison.OrdinalIgnoreCase)) ??
                                 state.AllowedValues.FirstOrDefault() ??
                                 value;

                return string.Format("<{0} xmlns:dt=\"urn:schemas-microsoft-com:datatypes\" dt:dt=\"{1}\">{2}</{0}>", argument.Name, state.DataType ?? "string", sendValue);
            }

            return string.Format("<{0}>{1}</{0}>", argument.Name, value);
        }

        private const string CommandBase = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" + "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" SOAP-ENV:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" + "<SOAP-ENV:Body>" + "<m:{0} xmlns:m=\"{1}\">" + "{2}" + "</m:{0}>" + "</SOAP-ENV:Body></SOAP-ENV:Envelope>";
    }
}
