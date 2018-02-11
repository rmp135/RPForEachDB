using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using RPForEachDB.Properties;
using System.Security;

namespace RPForEachDB
{
    public class ConnectionFactory
    {
        public SqlConnection Build(IServerModel serverModel)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder
            {
                ConnectTimeout = 5,
                DataSource = serverModel.Server,
                IntegratedSecurity = serverModel.AuthenticationMode == AuthenticationMode.Windows,
                UserID = serverModel.Username,
                Password = serverModel.Password
            };
            SqlConnection connection;
            connection = new SqlConnection(connectionStringBuilder.ToString());
            if (connection.State == System.Data.ConnectionState.Closed)
            {
                connection.Open();
            }
            return connection;
        }
    }
}
