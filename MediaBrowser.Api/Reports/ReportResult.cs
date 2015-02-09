using System.Collections.Generic;

namespace MediaBrowser.Api.Reports
{
    public class ReportResult
    {
        public List<List<string>> Rows { get; set; }
        public List<ReportFieldType> Columns { get; set; }

        public ReportResult()
        {
            Rows = new List<List<string>>();
            Columns = new List<ReportFieldType>();
        }
    }
}
