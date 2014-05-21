using MediaBrowser.Dlna.Common;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace MediaBrowser.Dlna.Service
{
    public class ServiceXmlBuilder
    {
        public string GetXml(IEnumerable<ServiceAction> actions, IEnumerable<StateVariable> stateVariables)
        {
            var builder = new StringBuilder();

            builder.Append("<?xml version=\"1.0\"?>");
            builder.Append("<scpd xmlns=\"urn:schemas-upnp-org:service-1-0\">");

            builder.Append("<specVersion>");
            builder.Append("<major>1</major>");
            builder.Append("<minor>0</minor>");
            builder.Append("</specVersion>");

            AppendActionList(builder, actions);
            AppendServiceStateTable(builder, stateVariables);

            builder.Append("</scpd>");

            return builder.ToString();
        }

        private void AppendActionList(StringBuilder builder, IEnumerable<ServiceAction> actions)
        {
            builder.Append("<actionList>");

            foreach (var item in actions)
            {
                builder.Append("<action>");

                builder.Append("<name>" + SecurityElement.Escape(item.Name ?? string.Empty) + "</name>");

                builder.Append("<argumentList>");

                foreach (var argument in item.ArgumentList)
                {
                    builder.Append("<argument>");

                    builder.Append("<name>" + SecurityElement.Escape(argument.Name ?? string.Empty) + "</name>");
                    builder.Append("<direction>" + SecurityElement.Escape(argument.Direction ?? string.Empty) + "</direction>");
                    builder.Append("<relatedStateVariable>" + SecurityElement.Escape(argument.RelatedStateVariable ?? string.Empty) + "</relatedStateVariable>");

                    builder.Append("</argument>");
                }

                builder.Append("</argumentList>");

                builder.Append("</action>");
            }

            builder.Append("</actionList>");
        }

        private void AppendServiceStateTable(StringBuilder builder, IEnumerable<StateVariable> stateVariables)
        {
            builder.Append("<serviceStateTable>");

            foreach (var item in stateVariables)
            {
                var sendEvents = item.SendsEvents ? "yes" : "no";

                builder.Append("<stateVariable sendEvents=\"" + sendEvents + "\">");

                builder.Append("<name>" + SecurityElement.Escape(item.Name ?? string.Empty) + "</name>");
                builder.Append("<dataType>" + SecurityElement.Escape(item.DataType ?? string.Empty) + "</dataType>");

                if (item.AllowedValues.Count > 0)
                {
                    builder.Append("<allowedValueList>");
                    foreach (var allowedValue in item.AllowedValues)
                    {
                        builder.Append("<allowedValue>" + SecurityElement.Escape(allowedValue) + "</allowedValue>");
                    }
                    builder.Append("</allowedValueList>");
                }

                builder.Append("</stateVariable>");
            }

            builder.Append("</serviceStateTable>");
        }
    }
}
