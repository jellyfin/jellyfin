using System.Linq;
using System.Text;

namespace MediaBrowser.Api.Reports
{
	/// <summary> A report export. </summary>
	public class ReportExport
	{
		/// <summary> Export to CSV. </summary>
		/// <param name="reportResult"> The report result. </param>
		/// <returns> A string. </returns>
		public string ExportToCsv(ReportResult reportResult)
		{
			StringBuilder returnValue = new StringBuilder();

			returnValue.AppendLine(string.Join(";", reportResult.Headers.Select(s => s.Name.Replace(',', ' ')).ToArray()));

			if (reportResult.IsGrouped)
				foreach (ReportGroup group in reportResult.Groups)
				{
					foreach (ReportRow row in reportResult.Rows)
					{
						returnValue.AppendLine(string.Join(";", row.Columns.Select(s => s.Name.Replace(',', ' ')).ToArray()));
					}
				}
			else
				foreach (ReportRow row in reportResult.Rows)
				{
					returnValue.AppendLine(string.Join(";", row.Columns.Select(s => s.Name.Replace(',', ' ')).ToArray()));
				}

			return returnValue.ToString();
		}


		/// <summary> Export to excel. </summary>
		/// <param name="reportResult"> The report result. </param>
		/// <returns> A string. </returns>
		public string ExportToExcel(ReportResult reportResult)
		{

			string style = @"<style type='text/css'>
							BODY {
									font-family: Arial;
									font-size: 12px;
								}

								TABLE {
									font-family: Arial;
									font-size: 12px;
								}

								A {
									font-family: Arial;
									color: #144A86;
									font-size: 12px;
									cursor: pointer;
									text-decoration: none;
									font-weight: bold;
								}
								DIV {
									font-family: Arial;
									font-size: 12px;
									margin-bottom: 0px;
								}
								P, LI, DIV {
									font-size: 12px;
									margin-bottom: 0px;
								}

								P, UL {
									font-size: 12px;
									margin-bottom: 6px;
									margin-top: 0px;
								}

								H1 {
									font-size: 18pt;
								}

								H2 {
									font-weight: bold;
									font-size: 14pt;
									COLOR: #C0C0C0;
								}

								H3 {
									font-weight: normal;
									font-size: 14pt;
									text-indent: +1em;
								}

								H4 {
									font-size: 10pt;
									font-weight: normal;
								}

								H5 {
									font-size: 10pt;
									font-weight: normal;
									background: #A9A9A9;
									COLOR: white;
									display: inline;
								}

								H6 {
									padding: 2 1 2 5;
									font-size: 11px;
									font-weight: bold;
									text-decoration: none;
									margin-bottom: 1px;
								}

								UL {
									line-height: 1.5em;
									list-style-type: disc;
								}

								OL {
									line-height: 1.5em;
								}

								LI {
									line-height: 1.5em;
								}

								A IMG {
									border: 0;
								}

								table.gridtable {
									color: #333333;
									border-width: 0.1pt;
									border-color: #666666;
									border-collapse: collapse;
								}

								table.gridtable th {
									border-width: 0.1pt;
									padding: 8px;
									border-style: solid;
									border-color: #666666;
									background-color: #dedede;
								}
								table.gridtable tr {
									background-color: #ffffff;
								}
								table.gridtable td {
									border-width: 0.1pt;
									padding: 8px;
									border-style: solid;
									border-color: #666666;
									background-color: #ffffff;
								}
						</style>";

			string Html = @"<!DOCTYPE html>
							<html xmlns='http://www.w3.org/1999/xhtml'>
							<head>
							<meta http-equiv='X-UA-Compatible' content='IE=8, IE=9, IE=10' />
							<meta charset='utf-8'>
							<title>Emby Reports Export</title>";
			Html += "\n" + style + "\n";
			Html += "</head>\n";
			Html += "<body>\n";

			StringBuilder returnValue = new StringBuilder();
			returnValue.AppendLine("<table  class='gridtable'>");
			returnValue.AppendLine("<tr>");
			returnValue.AppendLine(string.Join("", reportResult.Headers.Select(s => string.Format("<th>{0}</th>", s.Name)).ToArray()));
			returnValue.AppendLine("</tr>");
			if (reportResult.IsGrouped)
				foreach (ReportGroup group in reportResult.Groups)
				{
					returnValue.AppendLine("<tr>");
					returnValue.AppendLine("<th scope='rowgroup' colspan='" + reportResult.Headers.Count + "'>" + (string.IsNullOrEmpty(group.Name) ? "&nbsp;" : group.Name) + "</th>");
					returnValue.AppendLine("</tr>");
					foreach (ReportRow row in group.Rows)
					{
						ExportToExcelRow(reportResult, returnValue, row);
					}
					returnValue.AppendLine("<tr>");
					returnValue.AppendLine("<th style='background-color: #ffffff;' scope='rowgroup' colspan='" + reportResult.Headers.Count + "'>" + "&nbsp;" + "</th>");
					returnValue.AppendLine("</tr>");
				}

			else
				foreach (ReportRow row in reportResult.Rows)
				{
					ExportToExcelRow(reportResult, returnValue, row);
				}
			returnValue.AppendLine("</table>");

			Html += returnValue.ToString();
			Html += "</body>";
			Html += "</html>";
			return Html;
		}
		private static void ExportToExcelRow(ReportResult reportResult,
			StringBuilder returnValue,
			ReportRow row)
		{
			returnValue.AppendLine("<tr>");
			returnValue.AppendLine(string.Join("", row.Columns.Select(s => string.Format("<td>{0}</td>", s.Name)).ToArray()));
			returnValue.AppendLine("</tr>");
		}
	}

}
