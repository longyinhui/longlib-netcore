using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.Xml;

namespace longlib.cs.database
{
    /*
    public class DbConnector : IDisposable
    {
        public enum DbProvider
        {
            Oracle = 1,
            MSSQL = 2,
            OleDb = 3
        }

        public enum DbType
        {
            Oracle = 1,
            MSSQL = 2
        }

        protected const string CACHE_NAME = "DBCONNECTOR";

        public static string ConnectionString;
        public static DbProvider Provider;
        public static string User;
        public static string Password;
        public static string Source;
        public static bool Pooling;
        public static string DbName = "";
        //public static DbType Type;

        public static void Init(string connectionString, string provider)
        {
            Init(connectionString, (DbProvider)int.Parse(provider));
        }

        public static void Init(string connectionString, DbProvider provider)
        {
            ConnectionString = connectionString;
            Provider = provider;
            //Type = (DbType)Enum.Parse(typeof(DbType), dbType);
            ParseConnectionString();
        }

        protected static void ParseConnectionString()
        {
            string[] fields;
            string[] keyValue;

            fields = ConnectionString.Split(';');
            foreach (string field in fields)
            {
                if (field.Trim() == "") continue;
                keyValue = field.Split('=');
                if (keyValue.Length < 2)
                {
                    throw new Exception("Format of connection string incorrect!");
                }
                switch (keyValue[0].ToUpper())
                {
                    case "USER ID":
                        User = keyValue[1];
                        break;
                    case "PASSWORD":
                        Password = keyValue[1];
                        break;
                    case "DATA SOURCE":
                        Source = keyValue[1];
                        break;
                    case "INITIAL CATALOG":
                        DbName = keyValue[1];
                        break;
                    case "POOLING":
                        Pooling = keyValue[1].ToLower() == "true";
                        break;
                }
            }
        }

        protected IDbConnection Connection;
        protected IDbCommand Command;
        protected bool IsSubConnector;

        public string dateTimeFormat;

        public DbConnector()
        {
            switch (Provider)
            {
                case DbProvider.Oracle:
                    throw new NotImplementedException();
                case DbProvider.MSSQL:
                    throw new NotImplementedException();
                case DbProvider.OleDb:
                    //OleDbConnection and OleDbCommand are not supported in .Net Core
                    //Connection = new System.Data.OleDbConnection();
                    //Command = new OleDbCommand();
                    break;
            }
            Connection.ConnectionString = ConnectionString;

            Connection.Open();
            Command.Connection = Connection;

            IsSubConnector = false;
        }

        public DbConnector(DbConnector dbc)
        {
            switch (Provider)
            {
                case DbProvider.Oracle:
                    throw new NotImplementedException();

                case DbProvider.MSSQL:
                    throw new NotImplementedException();

                case DbProvider.OleDb:
                    Command = new OleDbCommand();
                    break;
            }
            this.Connection = null;
            this.Command.Connection = dbc.Connection;
            this.Command.Transaction = dbc.Command.Transaction;

            this.IsSubConnector = true;
        }

        public void Dispose()
        {
            if (IsSubConnector)
            {
                Command.Dispose();
            }
            else
            {
                RollbackTran();
                Command.Dispose();
                Connection.Close();
                Connection.Dispose();
            }
        }

        public void BeginTran()
        {
            Command.Transaction = Connection.BeginTransaction();
        }

        public void CommitTran()
        {
            if (Command.Transaction != null)
            {
                Command.Transaction.Commit();
                Command.Transaction = null;
            }
        }

        public void RollbackTran()
        {
            if (Command.Transaction != null)
            {
                Command.Transaction.Rollback();
                Command.Transaction = null;
            }
        }

        public IDbDataParameter CreateParameter(string name, System.Data.DbType type)
        {
            return CreateParameter(name, type, null);
        }

        public IDbDataParameter CreateParameter(string name, object value)
        {
            System.Data.DbType t;
            t = TypeConverter.ToDbType(value);
            if (DbProvider == DbProvider.Oracle && t == System.Data.DbType.String)
            {
                // For Oracle, AnsiString must be used, otherwise string functions on the parameter
                // would result in ORA-12704: character set mismatch
                t = System.Data.DbType.AnsiString;
            }
            return CreateParameter(name, t, value);
        }

        public IDbDataParameter CreateParameter(string name, System.Data.DbType type, object value)
        {
            IDbDataParameter param;

            name = name.ToUpper();
            if (Command.Parameters.Contains(name))
            {
                param = (IDbDataParameter)Command.Parameters[name];
            }
            else
            {
                param = Command.CreateParameter();
                param.ParameterName = name;
                Command.Parameters.Add(param);
            }
            param.DbType = type;
            if (value == null)
            {
                param.Value = DBNull.Value;
            }
            else if (value is bool)
            {
                param.Value = ((bool)value ? 1 : 0);
            }
            else
            {
                param.Value = value;
            }
            return param;
        }

        public void ClearParameters()
        {
            Command.Parameters.Clear();
        }

        public void RemoveParameter(IDbDataParameter param)
        {
            Command.Parameters.Remove(param);
        }

        public string CommandText
        {
            get { return Command.CommandText; }
            set
            {
                Command.CommandText = value;
            }
        }

        public IDbCommand GetCommand()
        {
            return this.Command;
        }

        public IDataReader ExecuteReader()
        {
            return Command.ExecuteReader();
        }

        public object ExecuteScalar()
        {
            return ExecuteScalar(false);
        }

        public object ExecuteScalar(bool enableCache)
        {
            string key = null;
            object value;

            if (enableCache)
            {
                key = GetCacheKey();
                value = CacheManagerWrap.GetCache(CACHE_NAME, key);
                if (value != null)
                {
                    return value;
                }
            }

            using (IDataReader dr = ExecuteReader())
            {
                if (!dr.Read()) return null;
                value = dr[0];
            }

            if (enableCache)
            {
                CacheManagerWrap.PutCache(CACHE_NAME, key, value);
            }
            return value;
        }

        public int ExecuteNonQuery()
        {
            int r;
            r = Command.ExecuteNonQuery();
            return r;
        }

        public int ExecuteProcedure()
        {
            string sql;
            int p1, p2;
            int r;

            switch (DbProvider)
            {
                case DbProvider.Oracle:
                    Command.CommandText = "BEGIN " + Command.CommandText + "; END;";
                    break;

                case DbProvider.MSSQL:
                    sql = Command.CommandText;
                    p1 = sql.IndexOf('(');
                    p2 = sql.LastIndexOf(')');
                    if (p1 > 0 && p2 > p1)
                    {
                        sql = sql.Remove(p2, 1).Remove(p1, 1).Insert(p1, " ");
                    }
                    Command.CommandText = "EXEC " + sql;
                    break;
            }

            r = Command.ExecuteNonQuery();

            return r;
        }

        protected string GetCacheKey()
        {
            string key;

            key = Command.CommandText;
            foreach (IDataParameter p in Command.Parameters)
            {
                key += "_" + TypeConverter.ToString(p.Value);
            }
            return key;
        }

        protected DataSet ExecuteSetByProvider()
        {
            DataSet ds = new DataSet();

            switch (DbProvider)
            {
                case DbProvider.Oracle:
                    throw new NotImplementedException();
                case DbProvider.MSSQL:
                    throw new NotImplementedException();
                case DbProvider.OleDb:
                    using (OleDbDataAdapter da = new OleDbDataAdapter((OleDbCommand)Command))
                    {
                        da.Fill(ds);
                    }
                    break;
            }
            return ds;
        }

        public DataSet ExecuteSet()
        {
            return ExecuteSet(false);
        }

        public DataSet ExecuteSet(bool enableCache)
        {
            DataSet ds;
            string key = null;

            if (enableCache)
            {
                key = GetCacheKey();
                ds = (DataSet)CacheManagerWrap.GetCache(CACHE_NAME, key);
                if (ds != null)
                {
                    return ds;
                }
            }

            ds = ExecuteSetByProvider();

            if (enableCache)
            {
                CacheManagerWrap.PutCache(CACHE_NAME, key, ds);
            }

            return ds;
        }

        public DataTable ExecuteTable()
        {
            return ExecuteTable(false);
        }

        public DataTable ExecuteTable(bool enableCache)
        {
            DataSet ds;

            ds = ExecuteSet(enableCache);
            if (ds.Tables.Count == 0)
            {
                return null;
            }
            return ds.Tables[0];
        }

        public DataRow ExecuteRow()
        {
            return ExecuteRow(false);
        }

        public DataRow ExecuteRow(bool enableCache)
        {
            DataTable dt;

            dt = ExecuteTable(enableCache);
            if (dt.Rows.Count == 0)
            {
                return null;
            }
            return dt.Rows[0];
        }

        public string ExecuteList()
        {
            return ExecuteList(false);
        }

        public string ExecuteList(bool enableCache)
        {
            DataTable dt;
            string list;
            int i;

            dt = ExecuteTable(enableCache);
            if (dt.Rows.Count == 0)
            {
                return null;
            }
            list = "";
            for (i = 0; i < dt.Rows.Count; i++)
            {
                if (i > 0) list += ",";
                list += TypeConverter.ToString(dt.Rows[i][0]);
            }
            return list;
        }

        public DataList ExecuteDataList()
        {
            return this.ExecuteDataList(false);
        }

        public DataList ExecuteDataList(bool enableCache)
        {
            IDataReader dr;
            DataList rows;
            DataLine row;
            string key = null;
            int rowid = 0;


            if (enableCache)
            {
                key = GetCacheKey();
                rows = (DataList)CacheManager_CS.GetCache(CACHE_NAME, key);
                if (rows != null)
                {
                    return rows;
                }
            }

            rows = new DataList();
            dr = ExecuteReader();
            while (dr.Read())
            {
                rowid++;
                row = new DataLine(dr.FieldCount);
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    row.Add(dr.GetName(i), dr[i]);
                }
                row.AddDataAttribute("rowStatus", "loaded");
                row.AddDataAttribute("rowId", rowid.ToString());
                rows.Add(row);
            }
            dr.Close();

            if (enableCache)
            {
                CacheManager_CS.PutCache(CACHE_NAME, key, rows);
            }
            return rows;
        }

        public DataTable ExecuteSchema()
        {
            DataTable dt;
            string key;

            key = GetCacheKey();
            dt = (DataTable)CacheManagerWrap.GetCache(CACHE_NAME, key);
            if (dt != null)
            {
                return dt;
            }

            dt = new DataTable();
            switch (DbProvider)
            {
                case DbProvider.Oracle:
                    throw new NotImplementedException();
                case DbProvider.MSSQL:
                    throw new NotImplementedException();
                case DbProvider.OleDb:
                    using (OleDbDataAdapter da = new OleDbDataAdapter((OleDbCommand)Command))
                    {
                        da.FillSchema(dt, SchemaType.Mapped);
                    }
                    break;
            }
            CacheManagerWrap.PutCache(CACHE_NAME, key, dt);
            return dt;
        }

        public int ExecuteXml(XmlNode nd, string tagName, bool upperCase)
        {
            int i, count = 0;
            XmlElement ndRow = (XmlElement)nd, ndValue;
            IDataReader dr = ExecuteReader();

            while (dr.Read())
            {
                if (!string.IsNullOrEmpty(tagName))
                {
                    ndRow = nd.OwnerDocument.CreateElement(tagName);
                    nd.AppendChild(ndRow);
                }
                for (i = 0; i < dr.FieldCount; i++)
                {
                    if (upperCase) ndValue = nd.OwnerDocument.CreateElement(dr.GetName(i).ToUpper());
                    else ndValue = nd.OwnerDocument.CreateElement(dr.GetName(i));
                    if (dr[i] != null)
                    {
                        if (dr[i].GetType() == typeof(DateTime))
                            ndValue.InnerText = TypeConverter.FormatDateTime((DateTime)dr[i], dateTimeFormat);
                        else
                            ndValue.InnerText = dr[i].ToString().Replace((char)2, '\0').Replace((char)8, '\0');
                    }
                    ndRow.AppendChild(ndValue);
                }
                count++;
            }
            dr.Close();
            return count;
        }

        public XmlNode ExecuteXml(string rowTagRoot, string cellTagName, bool upperCase)
        {
            int i, count = 0;
            XmlElement ndRow, ndValue;
            XmlDocument xmlDoc = new XmlDocument();
            IDataReader dr = ExecuteReader();

            if (string.IsNullOrEmpty(rowTagRoot)) rowTagRoot = "ROW";
            if (string.IsNullOrEmpty(cellTagName)) cellTagName = "LINE";
            xmlDoc.LoadXml("<" + rowTagRoot + "/>");
            while (dr.Read())
            {
                ndRow = xmlDoc.CreateElement(cellTagName);
                xmlDoc.AppendChild(ndRow);
                for (i = 0; i < dr.FieldCount - 1; i++)
                {
                    if (upperCase) ndValue = ndRow.OwnerDocument.CreateElement(dr.GetName(i).ToUpper());
                    else ndValue = ndRow.OwnerDocument.CreateElement(dr.GetName(i));
                    if (dr[i] != null)
                        ndValue.InnerText = dr[i].ToString();
                    ndRow.AppendChild(ndValue);
                }
                count++;
            }
            dr.Close();
            return xmlDoc;
        }
    }*/
}
