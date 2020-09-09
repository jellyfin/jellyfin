using System.Collections.Generic;
using System.Security;
using System.Text;
using Emby.Dlna.Common;

namespace Emby.Dlna.Service
{
    /// <summary>
    /// Defines the <see cref="ServiceXmlBuilder" />.
    /// </summary>
    public static class ServiceXmlBuilder
    {
        /// <summary>
        /// Returns a list of services that this instance provides.
        /// </summary>
        /// <param name="actions">The <see cref="IEnumerable{ServiceAction}"/>.</param>
        /// <param name="stateVariables">The <see cref="IEnumerable{StateVariable}"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string GetXml(IEnumerable<ServiceAction> actions, IEnumerable<StateVariable> stateVariables)
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

        /// <summary>
        /// Adds an action list.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/>.</param>
        /// <param name="actions">The <see cref="IEnumerable{ServiceAction}"/>.</param>
        private static void AppendActionList(StringBuilder builder, IEnumerable<ServiceAction> actions)
        {
            builder.Append("<actionList>");

            foreach (var item in actions)
            {
                builder.Append("<action>");

                builder.Append("<name>")
                    .Append(SecurityElement.Escape(item.Name ?? string.Empty))
                    .Append("</name>");

                builder.Append("<argumentList>");

                foreach (var argument in item.ArgumentList)
                {
                    builder.Append("<argument>");

                    builder.Append("<name>")
                        .Append(SecurityElement.Escape(argument.Name ?? string.Empty))
                        .Append("</name>");
                    builder.Append("<direction>")
                        .Append(SecurityElement.Escape(argument.Direction ?? string.Empty))
                        .Append("</direction>");
                    builder.Append("<relatedStateVariable>")
                        .Append(SecurityElement.Escape(argument.RelatedStateVariable ?? string.Empty))
                        .Append("</relatedStateVariable>");

                    builder.Append("</argument>");
                }

                builder.Append("</argumentList>");

                builder.Append("</action>");
            }

            builder.Append("</actionList>");
        }

        /// <summary>
        /// Adds a service state table.
        /// </summary>
        /// <param name="builder">The <see cref="StringBuilder"/>.</param>
        /// <param name="stateVariables">The <see cref="IEnumerable{StateVariable}"/>.</param>
        private static void AppendServiceStateTable(StringBuilder builder, IEnumerable<StateVariable> stateVariables)
        {
            builder.Append("<serviceStateTable>");

            foreach (var item in stateVariables)
            {
                var sendEvents = item.SendsEvents ? "yes" : "no";

                builder.Append("<stateVariable sendEvents=\"")
                    .Append(sendEvents)
                    .Append("\">");

                builder.Append("<name>")
                    .Append(SecurityElement.Escape(item.Name ?? string.Empty))
                    .Append("</name>");
                builder.Append("<dataType>")
                    .Append(SecurityElement.Escape(item.DataType ?? string.Empty))
                    .Append("</dataType>");

                if (item.AllowedValues.Count > 0)
                {
                    builder.Append("<allowedValueList>");
                    foreach (var allowedValue in item.AllowedValues)
                    {
                        builder.Append("<allowedValue>")
                            .Append(SecurityElement.Escape(allowedValue))
                            .Append("</allowedValue>");
                    }

                    builder.Append("</allowedValueList>");
                }

                builder.Append("</stateVariable>");
            }

            builder.Append("</serviceStateTable>");
        }
    }
}
