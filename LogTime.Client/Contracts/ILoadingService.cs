﻿namespace LogTime.Client.Contracts;

public interface ILoadingService
{
    void Show(string message);
    void Close();
}
