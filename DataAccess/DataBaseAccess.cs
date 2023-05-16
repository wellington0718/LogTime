using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataAccess;

public class DataBaseAccess
{
    private readonly IDbTransaction transaction;
    private readonly IDbConnection connection;

    public DataBaseAccess(IDbTransaction transaction)
    {
        this.transaction = transaction;
        connection = transaction.Connection;
    }

    public async Task<IEnumerable<T>> LoadDataAsync<T, TU>(string sql, TU parameters, CommandType? commandType = null)
    {
        return await connection.QueryAsync<T>(sql, parameters, transaction, commandType: commandType);
    }

    public async Task SaveDataAsync<T>(string sql, T parameters, CommandType? commandType = null)
    {
        await connection.ExecuteScalarAsync(sql, parameters, transaction, commandType: commandType);
    }

    public async Task<T> LoadFirstOrDefaultAsync<T, TU>(string sql, TU parameters, CommandType? commandType = null)
    {
        return await connection.QueryFirstOrDefaultAsync<T>(sql, parameters, transaction, commandType: commandType);
    }

    public async Task<T> ExecuteScalarAsync<T, TU>(string sql, TU parameters, CommandType? commandType = null)
    {
        return await connection.ExecuteScalarAsync<T>(sql, parameters, transaction, commandType: commandType);
    }

    public async Task<bool> ValidateAsync<T>(string sql, T parameters, CommandType? commandType = null)
    {
        return await connection.QuerySingleAsync<bool>(sql, parameters, transaction, commandType: commandType);
    }
}
