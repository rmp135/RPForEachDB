using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPForEachDB
{
    public class SQLTasks
    {
        private readonly IAppState _appState;

        public SQLTasks(IAppState appState)
        {
            _appState = appState;
        }

        public IEnumerable<string> GetAllDatabases(IDbConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = "SELECT Name FROM master.dbo.sysdatabases WHERE DATABASEPROPERTYEX(Name, 'Status') = 'ONLINE'";
            command.CommandTimeout = _appState.CommandTimeout;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                    yield return reader.GetString(0);
            }
        }

        public void Execute(IDbConnection connection, string commandText)
        {
            var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = commandText;
            command.CommandTimeout = _appState.CommandTimeout;
            command.ExecuteNonQuery();
        }
    }
}
