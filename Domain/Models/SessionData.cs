﻿namespace Domain.Models;

public class SessionData : BaseResponse
{
    public User User { get; set; }
    public LogHistory LogHistory { get; set; }
    public DateTime LoginTime { get; set; }
}
