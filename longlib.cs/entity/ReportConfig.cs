using System;
using System.Collections.Generic;
using System.Text;

namespace longlib.cs.entity
{
    public class ReportConfig
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public ReportDataType DataType { get; set; }
        public bool Enabled { get; set; }
        public string XsltTemplate { get; set; }
        public string XslfoTemplate { get; set; }
        public ReportDataSql[] DataSqls { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }

    public class ReportDataSql
    {
        public string SqlStatement { get; set; }
        public string Connection { get; set; }
        public string Tag { get; set; }
        public bool MultipleLine { get; set; }
    }

    public enum ReportDataType
    {
        XML = 1,
        JSON = 2,
        DataList = 3
    }

    public enum ReportFormat
    {
        HTML = 1,
        CSV = 2,
        Excel = 3,
        PDF = 4,
        PDF_FO = 5,
        JPEG = 6
    }
}
