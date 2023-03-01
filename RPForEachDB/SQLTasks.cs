using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace RPForEachDB;

public interface ISQLTasks
{
    IEnumerable<string> GetAllDatabases(IDbConnection connection);
    Task ExecuteAsync(IDbConnection connection, string commandText);
}

public class SQLTasks : ISQLTasks
{
    private readonly IConfigurationManager _configurationManager;
    private readonly SemaphoreSlim semaphore;
    public SQLTasks(IConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    public IEnumerable<string> GetAllDatabases(IDbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = @"
                SELECT Name FROM master.dbo.sysdatabases
                WHERE DATABASEPROPERTYEX(Name, 'Status') = 'ONLINE'
            ";
        command.CommandTimeout = _configurationManager.Configuration.CommandTimeout;
        using var reader = command.ExecuteReader();
        while (reader.Read())
            yield return reader.GetString(0);
    }

    public async Task ExecuteAsync(IDbConnection connection, string commandText)
    {
        var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = commandText;
        command.CommandTimeout = _configurationManager.Configuration.CommandTimeout;
        command.ExecuteNonQuery();
    }
}