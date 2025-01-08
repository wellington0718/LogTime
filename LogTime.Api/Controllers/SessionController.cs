using LogTime.Api.CustomExceptionHandler;

namespace LogTime.Api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]

public class SessionController(ILogTimeUnitOfWork logTimeUnitOfWork) : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Open([FromBody] ClientData clientData)
    {
        try
        {
            await logTimeUnitOfWork.CloseActiveSessionsAsync(clientData.LoggedOutBy, clientData.User);
            var newSessionData = await logTimeUnitOfWork.ActiveLogRepository.CreateNewSessionAsync(clientData);
            await logTimeUnitOfWork.CommitAsync();

            return CreateResponse(newSessionData);
        }
        catch (Exception ex)
        {
            throw LogTimeException.Throw(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Close([FromBody] SessionLogOutData sessionLogOutData)
    {
        try
        {
            var baseResponse = await logTimeUnitOfWork.CloseActiveSessionsAsync(sessionLogOutData.LoggedOutBy, sessionLogOutData.UserIds);
            return CreateResponse(baseResponse);
        }
        catch (Exception ex)
        {
            throw LogTimeException.Throw(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Update([FromBody] int logHistoryId)
    {
        try
        {
            var updateLogHistoryResponse = await logTimeUnitOfWork.UpdateLogHistoyAsync(logHistoryId);
            return CreateResponse(updateLogHistoryResponse);
        }
        catch (Exception ex)
        {
            throw LogTimeException.Throw(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult> Fetch([FromBody] string userId)
    {
        try
        {
            var fetchActiveSessionResponse = await logTimeUnitOfWork.FetchActiveSessionAsync(userId);
            return CreateResponse(fetchActiveSessionResponse);

        }
        catch (Exception ex)
        {
            throw LogTimeException.Throw(ex);
        }
    }

    [HttpPost]
    public async Task<ActionResult> ValidateCredentials([FromBody] ClientData clientData)
    {
        try
        {
            var validateCredentialResponse = await logTimeUnitOfWork.ValidateCredentialsAsync(clientData.User, clientData.Password);
            return CreateResponse(validateCredentialResponse);

        }
        catch (Exception ex)
        {
            throw LogTimeException.Throw(ex);
        }
    }
}
