using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace longlib.cs.data
{
    public class DataList : List<DataLine>
    {
        public DataList()
            : base()
        {
        }

        public DataList(int capacity)
            : base(capacity)
        {
        }

        public DataList(XmlDocument xmlList)
        {
            foreach (XmlElement xmlLine in xmlList.DocumentElement.ChildNodes)
            {
                this.Add(new DataLine(xmlLine));
            }
        }

        public DataList(JToken jtDataList)
        {
            foreach (JToken jtDataLine in jtDataList)
            {
                this.Add(new DataLine(jtDataLine));
            }
        }

        public DataList(string strJson)
        {
            JArray ja = JArray.Parse(strJson);
            foreach (JToken jtDataLine in ja)
            {
                this.Add(new DataLine(jtDataLine));
            }
        }

        //TODO
        /*
        public XmlDocument GetXml()
        {
            XmlDocument xmlList = new XmlDocument("DATA");
            foreach (DataLine dataLine in this)
            {
                dataLine.AppendToList(xmlList);
            }
            return xmlList;
        }
        */

        public delegate DataList DataListSelectDelegate(DataList dlObject);

        public string GetJson()
        {
            StringBuilder sb = new StringBuilder();
            JsonSerializerSettings jss = new JsonSerializerSettings();
            jss.DateFormatHandling = DateFormatHandling.MicrosoftDateFormat;
            bool blFirstRow = true;
            bool blFirstField = true;
            sb.Append("[");
            foreach (DataLine dl in this)
            {
                if (blFirstRow)
                {
                    blFirstRow = false;
                    sb.Append("{");
                }
                else
                {
                    sb.Append(",{");
                }
                sb.Append("row:{");
                blFirstField = true;
                foreach (KeyValuePair<string, object> item in dl)
                {
                    if (blFirstField)
                    {
                        blFirstField = false;
                    }
                    else
                    {
                        sb.Append(",");
                    }
                    if (item.Value is DateTime)
                    {
                        //TODO
                        //sb.AppendFormat("{0}:\"{1}\"", new object[] { item.Key + "_&D*", DateFormatter.ToJSTicks((DateTime)item.Value).ToString("0") });
                    }
                    else
                    {
                        sb.AppendFormat("{0}:{1}", new object[] { item.Key, JsonConvert.SerializeObject(item.Value) });
                    }
                }
                sb.Append("},");
                sb.Append("rowAttributes:{");
                blFirstField = true;
                foreach (KeyValuePair<string, string> item in dl.DataAttributes)
                {
                    if (blFirstField)
                    {
                        blFirstField = false;
                    }
                    else
                    {
                        sb.Append(",");
                    }
                    sb.AppendFormat("{0}:\"{1}\"", new string[] { item.Key, item.Value });
                }
                sb.Append("}");
                sb.Append("}");
            }
            sb.Append("]");
            return sb.ToString();
        }

        public string GetString(char cellSpliter, char rowSpliter)
        {
            StringBuilder sb = new StringBuilder();
            foreach (DataLine dataLine in this)
            {
                foreach (string key in dataLine.Keys)
                {
                    sb.Append(dataLine[key].ToString());
                    sb.Append(cellSpliter);
                }
                sb.Append(rowSpliter);
            }
            return sb.ToString();
        }
    }

    public class DataLine : Dictionary<string, object>
    {
        public DataLine()
            : base()
        {
        }

        public DataLine(int capacity)
            : base(capacity)
        {
        }

        public DataLine(XmlElement xmlLine)
        {
            this.InitData((XmlElement)xmlLine);
        }

        public DataLine(XmlDocument xmlList)
        {
            this.InitData((XmlElement)xmlList.DocumentElement.FirstChild);
        }

        public DataLine(JToken jtDataLine)
        {
            if (this.p_DataAttributes == null)
            {
                ////TODO
                //this.p_DataAttributes = new Dictionary<string, string>(jtDataLine["rowAttributes"].any());
            }
            foreach (JProperty jtCell in jtDataLine["row"])
            {
                if (jtCell.Name.IndexOf("_&D*") > 0)
                {
                    //TODO
                    //this.Add(jtCell.Name.Replace("_&D*", ""), DateFormatter.JSTicksToDateTime(jtCell.Value.ToString()));
                }
                else
                {
                    this.Add(jtCell.Name, jtCell.Value.ToString());
                }
            }
            foreach (JProperty jtAttribute in jtDataLine["rowAttributes"])
            {
                this.p_DataAttributes.Add(jtAttribute.Name, jtAttribute.Value.ToString());
            }
        }

        private void InitData(XmlElement xmlLine)
        {
            if (this.p_DataAttributes == null)
            {
                this.p_DataAttributes = new Dictionary<string, string>(xmlLine.Attributes.Count);
            }
            foreach (XmlNode xmlCell in xmlLine.ChildNodes)
            {
                this.Add(xmlCell.Name, xmlCell.InnerText);
            }
            foreach (XmlAttribute xmlAttribs in xmlLine.Attributes)
            {
                this.p_DataAttributes.Add(xmlAttribs.Name, xmlAttribs.Value);
            }
        }

        public Object this[int index]
        {
            get
            {
                int i = 0;
                foreach (KeyValuePair<string, object> item in this)
                {
                    if (i == index)
                    {
                        return item.Value;
                    }
                    i++;
                }
                throw new Exception("The index is out of range!");
            }
        }

        public string GetKey(int index)
        {
            string strKey = "";
            int i = 0;
            foreach (KeyValuePair<string, object> item in this)
            {
                if (i == index)
                {
                    strKey = item.Key;
                    break;
                }
                i++;
            }
            return strKey;
        }

        public DataLine SetIfEmpty(string key, object value)
        {
            if (!this.ContainsKey(key) || string.IsNullOrEmpty(this[key].ToString()))
            {
                this[key] = value;
            }
            return this;
        }

        private Dictionary<string, string> p_DataAttributes = new Dictionary<string, string>();

        public Dictionary<string, string> DataAttributes
        {
            get { return p_DataAttributes; }
            set { p_DataAttributes = value; }
        }

        public void AddDataAttribute(string key, string value)
        {
            this.p_DataAttributes.Add(key, value);
        }

        //TODO
        /*
        public XmlElement AppendToList(XmlDocument xmlList)
        {
            XmlElement eleLine = xmlList.Add("LINE");
            foreach (KeyValuePair<string, string> dataAttrib in this.p_DataAttributes)
            {
                eleLine.SetAttribute(dataAttrib.Key, dataAttrib.Value);
            }
            foreach (KeyValuePair<string, object> dataCell in this)
            {
                eleLine.Add(dataCell.Key, dataCell.Value);
            }
            return eleLine;
        }

        public XmlDocument GetXml()
        {
            XmlDocument xmlList = new XmlDocument("DATA");
            XmlElement eleLine = xmlList.Add("LINE");
            foreach (KeyValuePair<string, string> dataAttrib in this.p_DataAttributes)
            {
                eleLine.SetAttribute(dataAttrib.Key, dataAttrib.Value);
            }
            foreach (KeyValuePair<string, object> dataCell in this)
            {
                eleLine.Add(dataCell.Key, dataCell.Value);
            }
            return xmlList;
        }*/
    }

    public class DataListCollection : Dictionary<string, DataList>
    {
        public DataListCollection()
            : base()
        {
        }

        public DataListCollection(int capacity)
            : base(capacity)
        {
        }

        public DataListCollection(XmlDocument xmlLists, string keyName)
        {
            foreach (XmlDocument xmlList in xmlLists.ChildNodes)
            {
                this.Add(xmlList[keyName].Value, new DataList(xmlList));
            }
        }

        public DataListCollection(JToken jtDataLists)
        {
            foreach (JToken jtDataLine in jtDataLists)
            {
                //this.Add(new DataList(jtDataLine.));
            }
        }

        public DataListCollection(string strJson)
        {
            JArray ja = JArray.Parse(strJson);
            foreach (JToken jtDataLists in ja)
            {
                foreach (JToken jtDataList in jtDataLists)
                {
                    this.Add(((JProperty)jtDataList).Name, new DataList(jtDataLists[((JProperty)jtDataList).Name]));
                }

            }
        }
    }

}
