using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.Xml;

namespace longlib.database
{
    public class DbHelper : IDisposable
    {
        public IDbConnection Connection { get; set; }

        public IDbCommand Command { get; set; }

        private bool _transactionBegan = false;

        public DbHelper() 
        { }

        public DbHelper(IDbConnection connection) 
        {
            Connection = connection;
        }

        public void Dispose()
        {
            Command?.Dispose();
            Connection.Dispose();
        }

        public void BeginTransaction()
        {
            Connection.Open();
            Command = Connection.CreateCommand();
            Command.Connection = Connection;
            Command.Transaction = Connection.BeginTransaction();
            _transactionBegan = true;
        }

        public void Commit()
        {
            if (Command.Transaction != null && _transactionBegan)
            {
                Command.Transaction.Commit();
                Command.Transaction = null;            
            }
        }

        public void Rollback()
        {
            if (Command.Transaction != null && _transactionBegan)
            {
                Command.Transaction.Rollback();
                Command.Transaction = null;
            }
        }

        //private delegate int Exec();
        private T Execute<T>(string sqlText, CommandType commandType, Func<T> executeFunc, params IDbDataParameter[] parms)
        {
            T result;
            ConnectionState originalState = Connection.State;

            if (originalState != ConnectionState.Open)
                Connection.Open();
            try
            {
                if (!_transactionBegan)
                    Command = Connection.CreateCommand();
                Command.CommandText = sqlText;
                Command.CommandType = commandType;
                if (parms != null && parms.Length > 0)
                {
                    foreach (IDbDataParameter parm in parms)
                    {
                        Command.Parameters.Add(parm);
                    }
                }
                result = executeFunc();//Command.ExecuteNonQuery();
            }
            finally
            {
                if (originalState == ConnectionState.Closed)
                    Connection.Close();
            }
            return result;
        }

        public int ExecuteNonQuery(string sqlText, CommandType commandType, params IDbDataParameter[] parms)
        {
            return Execute(sqlText, commandType, () => Command.ExecuteNonQuery(), parms);
        }

        public IDataReader ExecuteReader(string sqlText, CommandType commandType, params IDbDataParameter[] parms)
        {
            Connection.Open();//Connection should keep open until DataReader.Read() completes
            return Execute(sqlText, commandType, () => Command.ExecuteReader(), parms);
        }

        public void OracleSqlLoader(string path, string tableName, Encoding encoding = null, string delimiter = ";", string lineBreak = "")
        {
            if (Command.GetType() != typeof(OracleCommand))
                throw new Exception("This function only support Oracle Database.");

            //copy source data
            string txtFilename = path + ".txt";
            StreamReader sr;

            if (encoding == null)
                File.Copy(path, txtFilename, true);
            else
            {
                using (sr = new StreamReader(path, encoding))
                using (StreamWriter sw = new StreamWriter(txtFilename, false, encoding))
                {
                    char[] buffer = new char[50000];
                    int read;
                    while (sr.Peek() >= 0)
                    {
                        read = sr.Read(buffer, 0, buffer.Length);
                        if (read > 0) sw.Write(buffer, 0, read);
                    }
                }
            }

            //get destination table structure
            OracleDataAdapter adapter = new OracleDataAdapter((OracleCommand)Command);
            DataTable dt = new DataTable();
            adapter.FillSchema(dt, SchemaType.Mapped);

            //generate .ctl file
            using (FileStream fs = new FileStream(txtFilename + ".ctl", FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write("load data\r\n");
                sw.Write("CHARACTERSET UTF8\r\n");
                sw.Write("infile '" + txtFilename + "' " + (string.IsNullOrEmpty(lineBreak) ? "" : "\"str X'" + BitConverter.ToString(Encoding.UTF8.GetBytes(lineBreak)).Replace("-", "") + "'\"") + "\r\n");
                sw.Write("append into table " + tableName + "\r\n");
                sw.Write("fields terminated by '" + delimiter + "\r\n");
                sw.Write("TRAILING NULLCOLS\r\n");

                StringBuilder ctlContent = new StringBuilder("(");
                string columnName, columnType;
                int typeLength;
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    columnName = dt.Columns[i].ColumnName.ToUpper();
                    columnType = dt.Columns[i].DataType.Name.ToUpper();
                    typeLength = dt.Columns[i].MaxLength;
                    switch (columnType)
                    {
                        case "DATETIME":
                            ctlContent.Append(columnName + " DATE(20) \"YYYY-MM-DD HH24:MI:SS\"," + (i == dt.Columns.Count - 1 ? " NULLIF (" + columnName + "=\"\")" : ","));
                            break;
                        case "STRING":
                            ctlContent.Append(columnName + " CHAR(" + (typeLength >= 1024000 ? 1024000 : typeLength) + ")" + (i == dt.Columns.Count - 1 ? " NULLIF (" + columnName + "=\"\")" : ","));
                            break;
                        default:
                            ctlContent.Append(columnName + (i == dt.Columns.Count - 1 ? " NULLIF (" + columnName + "=\"\")" : ","));
                            break;
                    }
                }
                ctlContent.Append(")");
                sw.Write(ctlContent);
            }

            //Process sqlldr.exe
            string command, userId, password, dataSource;
            int exitCode;
            ParseConnectString(Connection.ConnectionString, out userId, out password, out dataSource);
            command = "userid=" + userId + "/" + password + "@" + dataSource + " control=" + txtFilename + ".ctl" + " log=" + txtFilename + ".log" + " bad=" + txtFilename + ".bad";
            
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "sqlldr.exe";
                process.StartInfo.Arguments = command;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
                process.WaitForExit(3600000);
                if (!process.HasExited)
                { 
                    process.Kill();
                    throw new Exception("Run SQL*Loader (sqlldr.exe) failed.");
                }
                exitCode = process.ExitCode;
            }

            //Handle files
            if (File.Exists(txtFilename))
                File.Delete(txtFilename);
            if (File.Exists(txtFilename + ".ctl"))
                File.Delete(txtFilename + ".ctl");

            string message = "";
            if (File.Exists(txtFilename + ".bad"))
            {
                using (sr = new StreamReader(txtFilename + ".bad"))
                {
                    if (sr.Peek() > -1)
                        message += "1st bad line: " + sr.ReadLine() + ".\r\n";
                }
                File.Delete(txtFilename + ".bad");
            }
            else if (exitCode != 0)
                message += "SQL*Loader exit with error (" + exitCode + ").\r\n";
            
            if (!string.IsNullOrEmpty(message))
            {
                if (File.Exists(txtFilename + ".log"))
                    using (sr = new StreamReader(txtFilename + ".log"))
                    {
                        message += "Error log: " + sr.ReadToEnd() + ";";
                    }
                throw new Exception(message);
            }
        }

        public void MsSqlBcp()
        {

        }

        public int MySqlLoadDataInfile(string path, string tableName, Encoding encoding = null, string delimiter = ";", string lineBreak = "")
        {
            if (Connection.GetType() != typeof(MySqlConnection))
                throw new Exception("This function only support MySql Database.");

            string sqlText = "LOAD DATA INFILE '" + path + "'" + " INTO TABLE " + tableName;
            if (!string.IsNullOrEmpty(delimiter)) sqlText += " FIELD TERMINATED BY '" + delimiter + "'";

            //select * from demo01 into outfile '/var/lib/mysql-files/test01.txt'
            return ExecuteNonQuery(sqlText, CommandType.Text, null);
        }

        public int MySqlSelectOutFile(string sqlText, string path, string delimiter = ",", string lineBreak = "")
        {
            if (Connection.GetType() != typeof(MySqlConnection))
                throw new Exception("This function only support MySql Database.");

            sqlText += " INTO OUTFILE '" + path + "'";
            //select * from demo01 into outfile '/var/lib/mysql-files/test01.txt'
            return ExecuteNonQuery(sqlText, CommandType.Text, null);
        }

        public static void ParseConnectString(string connectionString, out string userId, out string password, out string dataSource)
        {
            userId = ""; password = ""; dataSource = "";
            //SqlConnection connection string: Data Source=x.c;Initial Catalog=db;User ID=user;Password=password;
            //OracleConnection connection string: Data Source=(tns);User ID=user;Password=password;
            if (string.IsNullOrEmpty(connectionString))
                return;
            string[] keyValueArr = connectionString.ToUpper().Split(";"), keyValue;
            foreach (string s in keyValueArr)
            {
                keyValue = s.Split('=');
                if (keyValue.Length < 2) continue;
                switch (keyValue[0])
                {
                    case "DATA SOURCE":
                        dataSource = keyValue[1];
                        break;
                    case "USER ID":
                        userId = keyValue[1];
                        break;
                    case "PASSWORD":
                        password = keyValue[1];
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
