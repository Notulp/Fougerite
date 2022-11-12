using System;
using System.Data.SQLite;
using System.IO;

namespace Fougerite
{
    /// <summary>
    /// This class handles SQLite support for the simple script plugins.
    /// </summary>
    public class SQLiteConnector
    {
        private static SQLiteConnector _inst;
        public string SQLitePath = $"{Directory.GetCurrentDirectory()}\\Save\\FougeriteSQL.sqlite";
        private SQLiteConnection _dbConnection;

        internal void Setup()
        {
            try
            {
                string extDir = $"{Directory.GetCurrentDirectory()}\\rust_server_Data\\Managed";
                File.WriteAllText(Path.Combine(extDir, "System.Data.SQLite.dll.config"), $"<configuration>\n<dllmap dll=\"sqlite3\" target=\"{extDir}\\x86\\libsqlite3.so\" os=\"!windows,osx\" cpu=\"x86\" />\n<dllmap dll=\"sqlite3\" target=\"{extDir}\\x64\\libsqlite3.so\" os=\"!windows,osx\" cpu=\"x86-64\" />\n</configuration>");
                if (!File.Exists(SQLitePath))
                {
                    SQLiteConnection.CreateFile(SQLitePath);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[SQLiteConnector] Failed: {ex}");
            }
        }

        /// <summary>
        /// Creates a new connection to the Database.
        /// You should check if Connection is null before calling this.
        /// </summary>
        /// <param name="extraarguments"></param>
        /// <returns></returns>
        public SQLiteConnection Connect(string extraarguments = ";Version=3;New=False;Compress=True;Foreign Keys=True;")
        {
            _dbConnection = new SQLiteConnection($"Data Source={SQLitePath}{extraarguments}");
            return _dbConnection;
        }

        /// <summary>
        /// Returns the current active connection to the database.
        /// </summary>
        public SQLiteConnection Connection
        {
            get { return _dbConnection; }
        }

        /// <summary>
        /// Creates an sql command.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public SQLiteCommand CreateSQLiteCommand(string command)
        {
            return new SQLiteCommand(command);
        }

        /// <summary>
        /// Creates an sql command for the connection.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="con"></param>
        /// <returns></returns>
        public SQLiteCommand CreateSQLiteCommand(string command, SQLiteConnection con)
        {
            return new SQLiteCommand(command, con);
        }

        /// <summary>
        /// Returns the instance of the class.
        /// </summary>
        public static SQLiteConnector GetInstance
        {
            get
            {
                if (_inst == null)
                {
                    _inst = new SQLiteConnector();
                }
                return _inst;
            }
        }
    }
}
