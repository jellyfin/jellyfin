using MediaBrowser.Dlna.Common;
using MediaBrowser.Model.Dlna;
using System.Collections.Generic;
using System.Security;
using System.Text;

namespace MediaBrowser.Dlna.Server
{
    public class ContentDirectoryXmlBuilder
    {
        private readonly DeviceProfile _profile;

        public ContentDirectoryXmlBuilder(DeviceProfile profile)
        {
            _profile = profile;
        }

        public string GetXml()
        {
            var builder = new StringBuilder();

            builder.Append("<?xml version=\"1.0\"?>");
            builder.Append("<scpd xmlns=\"urn:schemas-upnp-org:service-1-0\">");

            builder.Append("<specVersion>");
            builder.Append("<major>1</major>");
            builder.Append("<minor>0</minor>");
            builder.Append("</specVersion>");

            AppendActionList(builder);
            AppendServiceStateTable(builder);

            builder.Append("</scpd>");

            return builder.ToString();
        }

        private void AppendActionList(StringBuilder builder)
        {
            builder.Append("<actionList>");

            foreach (var item in new ServiceActionListBuilder().GetActions())
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

        private void AppendServiceStateTable(StringBuilder builder)
        {
            builder.Append("<serviceStateTable>");

            foreach (var item in GetStateVariables())
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

        private IEnumerable<StateVariable> GetStateVariables()
        {
            var list = new List<StateVariable>();

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_Filter",
                DataType = "string",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_SortCriteria",
                DataType = "string",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_Index",
                DataType = "ui4",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_Count",
                DataType = "ui4",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_UpdateID",
                DataType = "ui4",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "SearchCapabilities",
                DataType = "string",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "SortCapabilities",
                DataType = "string",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "SystemUpdateID",
                DataType = "ui4",
                SendsEvents = true
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_SearchCriteria",
                DataType = "string",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_Result",
                DataType = "string",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_ObjectID",
                DataType = "string",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_BrowseFlag",
                DataType = "string",
                SendsEvents = false,

                AllowedValues = new List<string>
                {
                    "BrowseMetadata",
                    "BrowseDirectChildren"
                }
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_BrowseLetter",
                DataType = "string",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_CategoryType",
                DataType = "ui4",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_RID",
                DataType = "ui4",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_PosSec",
                DataType = "ui4",
                SendsEvents = false
            });

            list.Add(new StateVariable
            {
                Name = "A_ARG_TYPE_Featurelist",
                DataType = "string",
                SendsEvents = false
            });
            
            return list;
        }

        public override string ToString()
        {
            return GetXml();
        }
    }
}
