using Microsoft.Data.SqlClient;

namespace RPForEachDB;

public interface IConnectionFactory
{
    SqlConnection Build(ServerModel serverModel);
}

public class ConnectionFactory : IConnectionFactory
{
    public SqlConnection Build(ServerModel serverModel)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder
        {
            ConnectTimeout = 5,
            DataSource = serverModel.Server,
            IntegratedSecurity = serverModel.AuthenticationMode == AuthenticationMode.Windows,
            UserID = serverModel.Username,
            TrustServerCertificate = true,
            Password = serverModel.Password
        };
        var connection = new SqlConnection(connectionStringBuilder.ToString());
        if (connection.State == System.Data.ConnectionState.Closed)
        {
            connection.Open();
        }
        return connection;
    }
}