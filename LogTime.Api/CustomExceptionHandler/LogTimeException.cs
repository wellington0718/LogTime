using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace LogTime.Api.CustomExceptionHandler;

public class LogTimeException(string message, string type, int errorCode) : Exception(message)
{
    public string Type { get; } = type;
    public int ErrorCode { get; } = errorCode;

    public static LogTimeException Throw(Exception ex) => ex switch
    {
        SqlException sqlEx => new LogTimeException(sqlEx.Message, nameof(SqlException), sqlEx.Number),
        DbException dbEx => new LogTimeException(dbEx.Message, nameof(DbException), -1),
        _ => new LogTimeException(ex.Message, nameof(Exception), 0),
    };
}
