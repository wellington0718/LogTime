using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess;

public interface ISqlServerDataAccess
{
    public string ConnectionString { get; set; }
    Task<bool> ValidateAsync<T>(string sql, T parameters, CommandType? commandType = null);
    Task<List<T>> LoadDataAsync<T, TU>(string sql, TU parameters, CommandType? commandType = null);
    Task SaveDataAsync<T>(string storedProcedure, T parameters, CommandType? commandType = null);
}

public class SqlServerDataAccess : ISqlServerDataAccess
{
    private readonly IDbTransaction transaction;

    public SqlServerDataAccess(IDbTransaction transaction)
    {
        this.transaction = transaction;
    }

    public string ConnectionString { get; set; }

    public async Task<bool> ValidateAsync<T>(string sql, T parameters, CommandType? commandType = null)
    {
        using IDbConnection connection = new SqlConnection(ConnectionString);

        var response = await connection.QuerySingleAsync<bool>(sql, parameters, commandType: commandType);

        return response;
    }

    public async Task<List<T>> LoadDataAsync<T, TU>(string sql, TU parameters, CommandType? commandType = null)
    {
        using IDbConnection connection = new SqlConnection(ConnectionString);

        var data =
            await connection.QueryAsync<T>(sql, parameters, commandType: commandType);
        return data.ToList();
    }

    public async Task SaveDataAsync<T>(string storedProcedure, T parameters, CommandType? commandType = null)
    {
        using var connection = transaction.Connection;

        await connection.ExecuteScalarAsync(storedProcedure, parameters, transaction, commandType: commandType);

        //return (int?)response ?? 0;
    }
}
