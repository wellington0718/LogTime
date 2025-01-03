﻿namespace Domain.Models;

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

public enum DialogBoxButton
{
    YesNo,
    Ok,
    OkCancel,
    RetryCancel
}

public enum AlertType
{
    Info,
    Warning,
    Error
}