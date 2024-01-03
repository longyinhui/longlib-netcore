using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.IO;
using longlib.cs.entity;
using longlib.cs.data;

namespace longlib.cs.report
{
    public class Report
    {
        private string id;
        private string filename;
        private string dataFilename;
        private Encoding encoding = Encoding.UTF8;
        private ReportConfig config;
        private ReportFormat format;

        private DataList data;

        private const string CsvDelimiter = ",";
        public Report(ReportConfig reportConfig, string dataSource)
        {
            config = reportConfig;
            ProcessData(dataSource);
            Generate();

        }
        public Report(ReportConfig reportConfig, DataList dataSource)
        {
        }
        public Report(ReportConfig reportConfig) : this(reportConfig, "")
        {
        }
        public Report(string configId, string dataSource) : this(GetReportConfig(configId), dataSource)
        {
        }
        public Report(string configId, DataList dataSource) : this(GetReportConfig(configId), dataSource)
        {
        }
        public Report(string configId): this(configId, "")
        {
        }

        private void ProcessData(string dataSource)
        {
            if (format == ReportFormat.CSV && string.IsNullOrEmpty(dataSource))
                return;
            switch (config.DataType)
            {
                case ReportDataType.JSON:
                    if (string.IsNullOrEmpty(dataSource))
                        LoadObjectDatabySql();
                    else
                        LoadObjectData(dataSource);
                        break;
                case ReportDataType.XML:
                    if (string.IsNullOrEmpty(dataSource))
                        WriteXmlDataFileBySql();
                    else
                        WriteXmlDataFile(dataSource);
                    break;
            }
        }
        private void ProcessData(DataList dataSource)
        {
        }

        public void Generate()
        {
            if (format == ReportFormat.CSV)
                GenerateCsv();
            else
                switch (config.DataType)
                {
                    case ReportDataType.JSON:
                    case ReportDataType.DataList:
                        Assemble();
                        break;
                    case ReportDataType.XML:
                        XslTransform();
                        break;
                }
        }

        private void WriteXmlDataFileBySql()
        {
            dataFilename = "C:\\Setups\\Data.txt";
            using (XmlTextWriter writer = new XmlTextWriter(dataFilename, encoding))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("root");

                writer.WriteStartElement("request");
                //TODO: HTML Request Tags?

                //Datetime Tag
                //writer.WriteStartElement("datetime"); writer.WriteString(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); writer.WriteEndElement();

                //Parameter Tags
                foreach (var parameter in config.Parameters)
                {
                    writer.WriteStartElement(parameter.Key);
                    writer.WriteString(parameter.Value);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();//for request

                foreach (ReportDataSql dataSql in config.DataSqls)
                {
                    writer.WriteStartElement(dataSql.Tag);
                    //TODO: Get XML from SQL
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();//for root
                writer.WriteEndDocument();
            }
        }
        private void WriteXmlDataFile(string xml)
        {
            XmlDocument document = new XmlDocument();
            XmlWriterSettings settings = new XmlWriterSettings();
            XmlWriter writer;

            dataFilename = "C:\\Setups\\Data.txt";
            document.LoadXml(xml);
            settings.OmitXmlDeclaration = true;
            settings.Encoding = encoding;
            using (writer = XmlWriter.Create(dataFilename, settings))
                document.WriteTo(writer);
        }
        private void XslTransform()
        {
            XslCompiledTransform transformer = new XslCompiledTransform();
            XmlWriterSettings settings = new XmlWriterSettings();
            XmlWriter writer;
            XmlDocument template = new XmlDocument();

            filename = "C:\\Setups\\Report.txt";
            template.LoadXml(format == ReportFormat.PDF_FO ? config.XslfoTemplate : config.XsltTemplate);
            //TODO: add style "border-collapse:collapse" for table?;
            transformer.Load(new XmlNodeReader(template.DocumentElement));
            settings.OmitXmlDeclaration = true;
            settings.Encoding = encoding;
            using (writer = XmlWriter.Create(filename, settings))
            {
                transformer.Transform(dataFilename, writer);
            }
            //If data file is temp file, delete it
            //File.Delete(dataFilename);
        }

        private void LoadObjectDatabySql()
        {
            //data = ...
        }
        private void LoadObjectData(string json)
        {
            //data = ...
        }
        private void Assemble()
        {

        }

        private void GenerateCsv()
        {
            if (config.DataSqls.Length > 0)
            {
                //execute Sql and generate
            }
            else if (data.Count > 0)
            {
                //Load data object and generate
            }
            else
            {
                //Load Xml and generate
            }
        }

        #region Database connection
        private static ReportConfig GetReportConfig(string configId)
        {
            ReportConfig reportConfig = new ReportConfig();
            return reportConfig;
        }
        #endregion
    }
}
