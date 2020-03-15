#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;
using Emby.Dlna.Service;

namespace Emby.Dlna.ContentDirectory
{
    public class ContentDirectoryXmlBuilder
    {
        public string GetXml()
        {
            return new ServiceXmlBuilder().GetXml(new ServiceActionListBuilder().GetActions(),
                GetStateVariables());
        }

        private static IEnumerable<StateVariable> GetStateVariables()
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

                AllowedValues = new string[]
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
    }
}
