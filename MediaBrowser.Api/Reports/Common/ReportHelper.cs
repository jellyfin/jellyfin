using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api.Reports
{
    /// <summary> A report helper. </summary>
    public class ReportHelper
    {
        #region [Public Methods]

        /// <summary> Convert field to string. </summary>
        /// <typeparam name="T"> Generic type parameter. </typeparam>
        /// <param name="value"> The value. </param>
        /// <param name="fieldType"> Type of the field. </param>
        /// <returns> The field converted to string. </returns>
        public static string ConvertToString<T>(T value, ReportFieldType fieldType)
        {
            if (value == null)
                return "";
            switch (fieldType)
            {
                case ReportFieldType.String:
                    return value.ToString();
                case ReportFieldType.Boolean:
                    return value.ToString();
                case ReportFieldType.Date:
                    return string.Format("{0:d}", value);
                case ReportFieldType.Time:
                    return string.Format("{0:t}", value);
                case ReportFieldType.DateTime:
                    return string.Format("{0:d}", value);
                case ReportFieldType.Minutes:
                    return string.Format("{0}mn", value);
                case ReportFieldType.Int:
                    return string.Format("", value);
                default:
                    if (value is Guid)
                        return string.Format("{0:N}", value);
                    return value.ToString();
            }
        }

        /// <summary> Gets filtered report header metadata. </summary>
        /// <param name="reportColumns"> The report columns. </param>
        /// <param name="defaultReturnValue"> The default return value. </param>
        /// <returns> The filtered report header metadata. </returns>
        public static List<HeaderMetadata> GetFilteredReportHeaderMetadata(string reportColumns, Func<List<HeaderMetadata>> defaultReturnValue = null)
        {
            if (!string.IsNullOrEmpty(reportColumns))
            {
                var s = reportColumns.Split('|').Select(x => ReportHelper.GetHeaderMetadataType(x)).Where(x => x != HeaderMetadata.None);
                return s.ToList();
            }
            else
                if (defaultReturnValue != null)
                    return defaultReturnValue();
                else
                    return new List<HeaderMetadata>();
        }

        /// <summary> Gets header metadata type. </summary>
        /// <param name="header"> The header. </param>
        /// <returns> The header metadata type. </returns>
        public static HeaderMetadata GetHeaderMetadataType(string header)
        {
            if (string.IsNullOrEmpty(header))
                return HeaderMetadata.None;

            HeaderMetadata rType;

            if (!Enum.TryParse<HeaderMetadata>(header, out rType))
                return HeaderMetadata.None;

            return rType;
        }

        /// <summary> Gets report view type. </summary>
        /// <param name="rowType"> The type. </param>
        /// <returns> The report view type. </returns>
        public static ReportViewType GetReportViewType(string rowType)
        {
            if (string.IsNullOrEmpty(rowType))
                return ReportViewType.ReportData;

            ReportViewType rType;

            if (!Enum.TryParse<ReportViewType>(rowType, out rType))
                return ReportViewType.ReportData;

            return rType;
        }

        /// <summary> Gets row type. </summary>
        /// <param name="rowType"> The type. </param>
        /// <returns> The row type. </returns>
        public static ReportIncludeItemTypes GetRowType(string rowType)
        {
            if (string.IsNullOrEmpty(rowType))
                return ReportIncludeItemTypes.BaseItem;

            ReportIncludeItemTypes rType;

            if (!Enum.TryParse<ReportIncludeItemTypes>(rowType, out rType))
                return ReportIncludeItemTypes.BaseItem;

            return rType;
        }

        /// <summary> Gets report display type. </summary>
        /// <param name="displayType"> Type of the display. </param>
        /// <returns> The report display type. </returns>
        public static ReportDisplayType GetReportDisplayType(string displayType)
        {
            if (string.IsNullOrEmpty(displayType))
                return ReportDisplayType.ScreenExport;

            ReportDisplayType rType;

            if (!Enum.TryParse<ReportDisplayType>(displayType, out rType))
                return ReportDisplayType.ScreenExport;

            return rType;
        }

        /// <summary> Gets core localized string. </summary>
        /// <param name="phrase"> The phrase. </param>
        /// <returns> The core localized string. </returns>
        public static string GetCoreLocalizedString(string phrase)
        {
            return BaseItem.LocalizationManager.GetLocalizedString(phrase);
        }

        #endregion

    }
}
