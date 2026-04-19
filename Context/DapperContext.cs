using System.Data;
using MySqlConnector;

namespace RealistikOsu.Cron.Context;

public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("MySqlConnection")!;
    }

    public IDbConnection CreateConnection()
        => new MySqlConnection(_connectionString);
}