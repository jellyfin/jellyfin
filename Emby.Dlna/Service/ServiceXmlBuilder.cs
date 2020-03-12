#pragma warning disable CS1591

using System.Collections.Generic;
using System.Text;
using Emby.Dlna.Common;
using Emby.Dlna.Server;

namespace Emby.Dlna.Service
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

        private static void AppendActionList(StringBuilder builder, IEnumerable<ServiceAction> actions)
        {
            builder.Append("<actionList>");

            foreach (var item in actions)
            {
                builder.Append("<action>");

                builder.Append("<name>" + DescriptionXmlBuilder.Escape(item.Name ?? string.Empty) + "</name>");

                builder.Append("<argumentList>");

                foreach (var argument in item.ArgumentList)
                {
                    builder.Append("<argument>");

                    builder.Append("<name>" + DescriptionXmlBuilder.Escape(argument.Name ?? string.Empty) + "</name>");
                    builder.Append("<direction>" + DescriptionXmlBuilder.Escape(argument.Direction ?? string.Empty) + "</direction>");
                    builder.Append("<relatedStateVariable>" + DescriptionXmlBuilder.Escape(argument.RelatedStateVariable ?? string.Empty) + "</relatedStateVariable>");

                    builder.Append("</argument>");
                }

                builder.Append("</argumentList>");

                builder.Append("</action>");
            }

            builder.Append("</actionList>");
        }

        private static void AppendServiceStateTable(StringBuilder builder, IEnumerable<StateVariable> stateVariables)
        {
            builder.Append("<serviceStateTable>");

            foreach (var item in stateVariables)
            {
                var sendEvents = item.SendsEvents ? "yes" : "no";

                builder.Append("<stateVariable sendEvents=\"" + sendEvents + "\">");

                builder.Append("<name>" + DescriptionXmlBuilder.Escape(item.Name ?? string.Empty) + "</name>");
                builder.Append("<dataType>" + DescriptionXmlBuilder.Escape(item.DataType ?? string.Empty) + "</dataType>");

                if (item.AllowedValues.Length > 0)
                {
                    builder.Append("<allowedValueList>");
                    foreach (var allowedValue in item.AllowedValues)
                    {
                        builder.Append("<allowedValue>" + DescriptionXmlBuilder.Escape(allowedValue) + "</allowedValue>");
                    }
                    builder.Append("</allowedValueList>");
                }

                builder.Append("</stateVariable>");
            }

            builder.Append("</serviceStateTable>");
        }
    }
}
