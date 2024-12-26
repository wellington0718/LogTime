namespace Domain.Models;

public enum ConnectionStringName
{
    LogTime
}

public enum StatusMessage
{
    Ok,
    Unauthorized,
    Error,
    NotAllowed,
    Success
}

public enum SharedStatus
{
    Break,
    Lunch,
    NoActivity,
}