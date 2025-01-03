namespace LogTime.Api.Contracts;

public interface IActiveLogRepository : IGenericRepository<ActiveLog>
{
    Task<SessionData> CreateNewSessionAsync(ClientData clientData);
    Task<bool> CloseActiveSessionsAsync(SessionLogOutData sessionLogOutData);
}
