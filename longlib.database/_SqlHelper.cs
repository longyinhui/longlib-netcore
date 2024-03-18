using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace longlib.database
{
    public interface ISqlHelper
    {
        /// <summary>
        /// 当前连接字符串
        /// </summary>
        IDbConnection Connection { get; set; }

        /// <summary>
        /// 查询
        /// </summary>
        /// <typeparam name="T">实体类</typeparam>
        /// <param name="sqlText">Sql文本</param>
        /// <param name="commandType">命令类型</param>
        /// <param name="parms">参数</param>
        /// <returns>返回泛型的实体对象</returns>
        IEnumerable<T> Query<T>(string sqlText, CommandType commandType = CommandType.Text, params IDbDataParameter[] parms) where T : class;

        /// <summary>
        /// 查询
        /// </summary>
        /// <typeparam name="T">实体类</typeparam>
        /// <param name="sqlText">Sql文本</param>
        /// <param name="func">返回IDbDataParameter[]的委托</param>
        /// <param name="commandType">命令类型</param>
        /// <returns>返回泛型的实体对象</returns>
        IEnumerable<T> Query<T>(string sqlText, Func<IDbDataParameter[]> func, CommandType commandType = CommandType.Text) where T : class;

        /// <summary>
        /// 查询首行首列
        /// </summary>
        /// <param name="sqlText">Sql文本</param>
        /// <param name="commandType">命令类型</param>
        /// <param name="parms">参数</param>
        /// <returns>首行首列的值</returns>
        object Scalar(string sqlText, CommandType commandType = CommandType.Text, params IDbDataParameter[] parms);

        /// <summary>
        /// 查询首行首列
        /// </summary>
        /// <param name="sqlText">Sql文本</param>
        /// <param name="func">返回IDbDataParameter[]的委托</param>
        /// <param name="commandType">命令类型</param>
        /// <returns>首行首列的值</returns>
        object Scalar(string sqlText, Func<IDbDataParameter[]> func, CommandType commandType = CommandType.Text);

        /// <summary>
        /// 查询首行首列
        /// </summary>
        /// <typeparam name="T">仅支持基础类型</typeparam>
        /// <param name="sqlText">Sql文本</param>
        /// <param name="commandType">命令类型</param>
        /// <param name="parms">参数</param>
        /// <returns></returns>
        T Scalar<T>(string sqlText, CommandType commandType = CommandType.Text, params IDbDataParameter[] parms) where T : struct;

        /// <summary>
        /// 查询首行首列
        /// </summary>
        /// <typeparam name="T">仅支持基础类型</typeparam>
        /// <param name="sqlText">Sql文本</param>
        /// <param name="func">返回IDbDataParameter[]的委托</param>
        /// <param name="commandType">命令类型</param>
        /// <returns>首行首列的值</returns>
        T Scalar<T>(string sqlText, Func<IDbDataParameter[]> func, CommandType commandType = CommandType.Text) where T : struct;


        /// <summary>
        /// 执行脚本
        /// </summary>
        /// <param name="sqlText">Sql文本</param>
        /// <param name="commandType">命令类型</param>
        /// <param name="parms">参数</param>
        /// <returns>返回影响行数</returns>
        int Execute(string sqlText, CommandType commandType = CommandType.Text, params IDbDataParameter[] parms);


        /// <summary>
        /// 执行脚本
        /// </summary>
        /// <param name="sqlText">Sql文本</param>
        /// <param name="func">返回IDbDataParameter[]的委托</param>
        /// <param name="commandType">命令类型</param>
        /// <returns></returns>
        int Execute(string sqlText, Func<IDbDataParameter[]> func, CommandType commandType = CommandType.Text);
    }

    public class SqlHelper : ISqlHelper, IDisposable
    {
        public SqlHelper(IDbConnection connection)
        {
            Connection = connection;
        }

        public IDbConnection Connection { get; set; }

        public void Dispose()
        {
            Connection.Dispose();
        }

        private IDbCommand buildCommand(string sqlText, CommandType commandType, params IDbDataParameter[] parms)
        {
            if (Connection == null)
                throw new Exception("IDbConnection is NULL");

            IDbCommand command = Connection.CreateCommand();
            command.CommandText = sqlText;
            command.CommandType = commandType;
            if (parms != null && parms.Length > 0)
            {
                foreach (var item in parms)
                {
                    command.Parameters.Add(item);
                }
            }
            return command;
        }

        public int Execute(string sqlText, CommandType commandType = CommandType.Text, params IDbDataParameter[] parms)
        {
            if (Connection == null)
                throw new Exception("IDbConnection is NULL");

            ConnectionState originalState = Connection.State;

            if (originalState != ConnectionState.Open)
                Connection.Open();
            try
            {
                return buildCommand(sqlText, commandType, parms).ExecuteNonQuery();
            }
            finally
            {
                if (originalState == ConnectionState.Closed)
                    Connection.Close();
            }
        }

        public int Execute(string sqlText, Func<IDbDataParameter[]> func, CommandType commandType = CommandType.Text)
        {
            return Execute(sqlText, commandType, func());
        }

        public IEnumerable<T> Query<T>(string sqlText, CommandType commandType = CommandType.Text, params IDbDataParameter[] parms) where T : class
        {
            if (Connection == null)
                throw new Exception("IDbConnection is NULL");

            ConnectionState originalState = Connection.State;

            if (originalState != ConnectionState.Open)
                Connection.Open();
            try
            {
                var reader = buildCommand(sqlText, commandType, parms).ExecuteReader();
                while (reader.Read())
                    yield return new List<T>()[0];//for building only
                    //yield return EntityBuilder<T>.GenerateByDataRecord(reader);
            }
            finally
            {
                if (originalState == ConnectionState.Closed)
                    Connection.Close();
            }
        }

        public IEnumerable<T> Query<T>(string sqlText, Func<IDbDataParameter[]> func, CommandType commandType = CommandType.Text) where T : class
        {
            return Query<T>(sqlText, commandType, func());
        }

        public object Scalar(string sqlText, CommandType commandType = CommandType.Text, params IDbDataParameter[] parms)
        {
            if (Connection == null)
                throw new Exception("IDbConnection is NULL");

            ConnectionState originalState = Connection.State;

            if (originalState != ConnectionState.Open)
                Connection.Open();
            try
            {
                return buildCommand(sqlText, commandType, parms).ExecuteScalar();
            }
            finally
            {
                if (originalState == ConnectionState.Closed)
                    Connection.Close();
            }
        }

        public object Scalar(string sqlText, Func<IDbDataParameter[]> func, CommandType commandType = CommandType.Text)
        {
            return Scalar(sqlText, commandType, func());
        }

        public T Scalar<T>(string sqlText, CommandType commandType = CommandType.Text, params IDbDataParameter[] parms) where T : struct
        {
            return (T)Scalar(sqlText, commandType, parms);
        }

        public T Scalar<T>(string sqlText, Func<IDbDataParameter[]> func, CommandType commandType = CommandType.Text) where T : struct
        {
            return Scalar<T>(sqlText, commandType, func());
        }
    }
}
