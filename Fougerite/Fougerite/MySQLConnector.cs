using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

//using MySql.Data.MySqlClient;

namespace Fougerite
{
    /// <summary>
    /// This class helps script plugins to use a simple MySQL connection.
    /// </summary>
    public class MySQLConnector
    {
        private static MySQLConnector _inst;

        private MySqlConnection connection;
        public string ServerAddress;
        public string DataBase;
        private string _username;
        private string _password;

        /// <summary>
        /// Connects to the mysql server with the given parameters.
        /// You should check if Connection is null before creating this.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="database"></param>
        /// <param name="username"></param>
        /// <param name="passwd"></param>
        /// <param name="extraarg"></param>
        /// <returns></returns>
        public MySqlConnection Connect(string ip, string database, string username, string passwd, string extraarg = "")
        {
            ServerAddress = ip;
            DataBase = database;
            _username = username;
            _password = passwd;
            string connectionString =
                $"SERVER={ServerAddress};DATABASE={DataBase};UID={_username};PASSWORD={_password};{extraarg}";

            connection = new MySqlConnection(connectionString);
            return connection;
        }

        /// <summary>
        /// Executes and sql query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool ExecuteNonQuery(string query)
        {
            try
            {
                MySqlCommand cmd = new MySqlCommand();
                cmd.CommandText = query;
                cmd.Connection = connection;
                cmd.ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                Logger.LogError($"Failed to execute query {ex}");
                return false;
            }
            return true;
        }
        
        
        /// <summary>
        /// README: Oracle implemented an async way, but there is no way to make a proper async callback
        /// as you would be doing in Web.CreateAsyncHTTPRequest. They completely left this API call out:
        /// this.asyncResult = this.caller.BeginInvoke(1, behavior, (AsyncCallback) null, (object) null);
        /// SOLUTION: Just run the mysql functions in a thread if you don't want to block the game's main thread really.
        /// This could be patched as well, and extending their shitty class within Fougerite, but I don't think It's worth
        /// the hassle unless needed.
        /// You may read their docs here below:
        /// 
        /// An System.IAsyncResult interface that represents the asynchronous operation started by calling this method.
        /// Remarks
        /// BeginExecuteReader method enables you to execute a query on a
        /// server without having current thread blocked.
        /// In other words, your program can continue execution while
        /// the query runs on MySQL so you do not have to wait for it.
        /// Refer to "Asynchronous Query Execution" article for detailed information.
        ///
        /// To start running a query, you have to call BeginExecuteReader method,
        /// which in turn invokes appropriate actions in another thread.
        /// Return value of this method must be assigned to an System.IAsyncResult object.
        /// After executing this method, the program flow continues.
        ///
        /// When you are ready to accept query results, call EndExecuteReader.
        /// If at the moment you call this function the query execution has not yet been finished, application stops and waits
        /// till the function returns. Then you can treat a DbDataReaderBase in a common way.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public Dictionary<string, object> ExecuteQuery(string query, Dictionary<string, object> parameters = null)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            try
            {
                MySqlCommand cmd = new MySqlCommand();
                cmd.CommandText = query;
                cmd.Connection = connection;
                if (parameters != null)
                {
                    foreach (var x in parameters)
                    {
                        cmd.Parameters.AddWithValue(x.Key, x.Value);
                    }
                }

                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int fieldCount = reader.FieldCount;
                    for (int i = 0; i < fieldCount; i++)
                    {
                        string key = reader.GetName(i);
                        object val = reader.GetValue(i);
                        result.Add(key, val);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to execute query {ex}");
            }
            return result;
        }

        /// <summary>
        /// Opens the SQL connection.
        /// </summary>
        /// <returns></returns>
        public bool OpenConnection()
        {
            try
            {
                if (connection != null)
                {
                    connection.Open();
                }
                return true;
            }
            catch (MySqlException ex)
            {
                switch (ex.Number)
                {
                    case 0:
                        Logger.LogError("Cannot connect to server.");
                        break;

                    case 1045:
                        Logger.LogError("Invalid username/password, please try again");
                        break;
                    default:
                        Logger.LogError($"Error: {ex}");
                        break;
                }
                return false;
            }
        }
        
        /// <summary>
        /// Closes the sql connection.
        /// </summary>
        /// <returns></returns>
        public bool CloseConnection()
        {
            try
            {
                if (connection != null) connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                Logger.LogError($"Failed to close connection {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a new SQL command.
        /// </summary>
        /// <returns></returns>
        public MySqlCommand CreateMysqlCommand()
        {
            return new MySqlCommand();
        }

        /// <summary>
        /// Returns the current connection.
        /// </summary>
        public MySqlConnection Connection
        {
            get { return connection; }
        }

        /// <summary>
        /// Returns the instance of the class.
        /// </summary>
        public static MySQLConnector GetInstance
        {
            get
            {
                if (_inst == null)
                {
                    _inst = new MySQLConnector();
                }
                return _inst;
            }
        }
    }
}
