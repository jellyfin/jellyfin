#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;
using Emby.Dlna.Service;

namespace Emby.Dlna.ContentDirectory
{
    public static class ContentDirectoryXmlBuilder
    {
        public static string GetXml()
        {
            return ServiceXmlBuilder.GetXml(
                ServiceActionListBuilder.GetActions(),
                GetStateVariables());
        }

        private static IEnumerable<StateVariable> GetStateVariables()
        {
            var list = new List<StateVariable>
            {
                new StateVariable
                {
                    Name = "A_ARG_TYPE_Filter",
                    DataType = "string",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_SortCriteria",
                    DataType = "string",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_Index",
                    DataType = "ui4",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_Count",
                    DataType = "ui4",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_UpdateID",
                    DataType = "ui4",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "SearchCapabilities",
                    DataType = "string",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "SortCapabilities",
                    DataType = "string",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "SystemUpdateID",
                    DataType = "ui4",
                    SendsEvents = true
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_SearchCriteria",
                    DataType = "string",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_Result",
                    DataType = "string",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_ObjectID",
                    DataType = "string",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_BrowseFlag",
                    DataType = "string",
                    SendsEvents = false,

                    AllowedValues = new[]
                    {
                        "BrowseMetadata",
                        "BrowseDirectChildren"
                    }
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_BrowseLetter",
                    DataType = "string",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_CategoryType",
                    DataType = "ui4",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_RID",
                    DataType = "ui4",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_PosSec",
                    DataType = "ui4",
                    SendsEvents = false
                },

                new StateVariable
                {
                    Name = "A_ARG_TYPE_Featurelist",
                    DataType = "string",
                    SendsEvents = false
                }
            };

            return list;
        }
    }
}
