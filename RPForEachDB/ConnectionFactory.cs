using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using RPForEachDB.Properties;

namespace RPForEachDB
{
    public class ConnectionFactory
    {
        public SqlConnection Build()
        {
            var connectionString = Settings.Default.ConnectionString;
            var connection = new SqlConnection(connectionString);
            if (connection.State == System.Data.ConnectionState.Closed)
                connection.Open();
            return connection;
        }
    }
}
