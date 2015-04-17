using MediaBrowser.Controller.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Reports
{
	public class ReportHelper
	{
		/// <summary> Gets java script localized string. </summary>
		/// <param name="phrase"> The phrase. </param>
		/// <returns> The java script localized string. </returns>
		public static string GetJavaScriptLocalizedString(string phrase)
		{
			var dictionary = BaseItem.LocalizationManager.GetJavaScriptLocalizationDictionary(BaseItem.ConfigurationManager.Configuration.UICulture);

			string value;

			if (dictionary.TryGetValue(phrase, out value))
			{
				return value;
			}

			return phrase;
		}

		/// <summary> Gets server localized string. </summary>
		/// <param name="phrase"> The phrase. </param>
		/// <returns> The server localized string. </returns>
		public static string GetServerLocalizedString(string phrase)
		{
			return BaseItem.LocalizationManager.GetLocalizedString(phrase, BaseItem.ConfigurationManager.Configuration.UICulture);
		}

		/// <summary> Gets row type. </summary>
		/// <param name="rowType"> The type. </param>
		/// <returns> The row type. </returns>
		public static ReportViewType GetRowType(string rowType)
		{
			if (string.IsNullOrEmpty(rowType))
				return ReportViewType.BaseItem;

			ReportViewType rType;

			if (!Enum.TryParse<ReportViewType>(rowType, out rType))
				return ReportViewType.BaseItem;

			return rType;
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
				case ReportFieldType.Int:
					return string.Format("", value);
				default:
					if (value is Guid)
						return string.Format("{0:N}", value);
					return value.ToString();
			}
		}
	}
}
