#pragma warning disable CS1591

using System.Collections.Generic;
using Emby.Dlna.Common;
using Emby.Dlna.Service;

namespace Emby.Dlna.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="ContentDirectoryXmlBuilder" />.
    /// </summary>
    public static class ContentDirectoryXmlBuilder
    {
        /// <summary>
        /// Gets the ContentDirectory:1 service template.
        /// See http://upnp.org/specs/av/UPnP-av-ContentDirectory-v1-Service.pdf.
        /// </summary>
        /// <returns>An XML description of this service.</returns>
        public static string GetXml()
        {
            return new ServiceXmlBuilder().GetXml(ServiceActionListBuilder.GetActions(), GetStateVariables());
        }

        /// <summary>
        /// Get the list of state variables for this invocation.
        /// </summary>
        /// <returns>The <see cref="IEnumerable{StateVariable}"/>.</returns>
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
