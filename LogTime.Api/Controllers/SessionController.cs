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
            await logTimeUnitOfWork.ActiveLogRepository.CloseActiveSessionsAsync(clientData.LoggedOutBy, clientData.User);
            var newSessionData = await logTimeUnitOfWork.ActiveLogRepository.CreateNewSessionAsync(clientData);
            await logTimeUnitOfWork.CommitAsync();

            return CreateResponse(newSessionData);
        }
        catch (Exception exception)
        {
            return CreateResponse(new BaseResponse
            {
                HasError = true,
                Code = StatusCodes.Status500InternalServerError,
                Title = nameof(StatusMessage.Error),
                Message = exception.GetBaseException().Message
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult> Close([FromBody] SessionLogOutData sessionLogOutData)
    {
        try
        {
            var idList = sessionLogOutData.UserIds.Split(',').ToList();
            await logTimeUnitOfWork.ActiveLogRepository.DeleteAsync(activeLog => idList.Contains(activeLog.UserId));
            await logTimeUnitOfWork.ActiveLogRepository.CloseActiveSessionsAsync(sessionLogOutData.LoggedOutBy, sessionLogOutData.UserIds);
            await logTimeUnitOfWork.CommitAsync();

            return CreateResponse(new BaseResponse
            {
                Code = StatusCodes.Status200OK,
                Message = nameof(StatusMessage.Success),
            });
        }
        catch (Exception exception)
        {
            return CreateResponse(new BaseResponse
            {
                HasError = true,
                Code = StatusCodes.Status500InternalServerError,
                Title = nameof(StatusMessage.Error),
                Message = exception.GetBaseException().Message
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult> Update([FromBody] int logHistoryId)
    {
        try
        {
            var logHistory = await logTimeUnitOfWork.LogHistoryRepository.FindAsync(logHistoryId);

            if (logHistory.LogoutDate.HasValue)
            {
                await logTimeUnitOfWork.ActiveLogRepository.DeleteAsync(logHistory => logHistory.Id == logHistoryId);
                await logTimeUnitOfWork.SaveChangesAsync();
                await logTimeUnitOfWork.CommitAsync();
                return CreateResponse(new BaseResponse
                {
                    Code = StatusCodes.Status200OK,
                    Title = nameof(StatusMessage.Ok),
                    IsSessionAlreadyClose = true,
                    Message = nameof(StatusMessage.Success)
                });
            }

            logHistory.LastTimeConnectionAlive = DateTime.Now;
            logTimeUnitOfWork.LogHistoryRepository.Update(logHistory);
            await logTimeUnitOfWork.SaveChangesAsync();
            await logTimeUnitOfWork.CommitAsync();

            return CreateResponse(new SessionAliveDate { LastDate = logHistory.LastTimeConnectionAlive });
        }
        catch (Exception exception)
        {
            return CreateResponse(new BaseResponse
            {
                HasError = true,
                Code = StatusCodes.Status500InternalServerError,
                Title = nameof(StatusMessage.Error),
                Message = exception.GetBaseException().Message
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult> Fetch([FromBody] string userId)
    {
        try
        {
            var foundLogHistory = await logTimeUnitOfWork.LogHistoryRepository.FindAsync(logHistory => logHistory.IdUser.Equals(userId) && logHistory.LogoutDate == null);

            var response = foundLogHistory == null
                ? new FetchSessionData { IsAlreadyOpened = false, IsSessionAlreadyClose = true }
                : new FetchSessionData { IsAlreadyOpened = true, CurrentRemoteHost = foundLogHistory.Hostname };

            return CreateResponse(response);

        }
        catch (Exception exception)
        {
            return CreateResponse(new BaseResponse
            {
                HasError = true,
                Code = StatusCodes.Status500InternalServerError,
                Title = nameof(StatusMessage.Error),
                Message = exception.GetBaseException().Message
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult> ValidateCredentials([FromBody] ClientData clientData)
    {
        try
        {
            var userExists = await logTimeUnitOfWork.UserRepository.ValidateCredentialsAsync(clientData.User, clientData.Password);

            if (!userExists)
            {
                return CreateResponse(new BaseResponse
                {
                    HasError = false,
                    Code = StatusCodes.Status401Unauthorized,
                    Title = nameof(StatusMessage.Unauthorized),
                    Message = nameof(StatusMessage.Success),
                });
            }
            //else
            //{
            //    var isNotAllowed = await logTimeUnitOfWork.UserRepository.IsUserNotAllowedAsync(clientData.User);

            //    if (isNotAllowed)
            //    {
            //        return CreateResponse(new BaseResponse
            //        {
            //            HasError = true,
            //            Code = StatusCodes.Status401Unauthorized,
            //            Title = nameof(StatusMessage.NotAllowed),
            //            Message = nameof(StatusMessage.Success)
            //        });
            //    }
            //}

            return CreateResponse(new BaseResponse
            {
                HasError = false,
                Code = StatusCodes.Status200OK,
                Title = nameof(StatusMessage.Ok),
                Message = nameof(StatusMessage.Success)
            });
        }
        catch (Exception exception)
        {
            return CreateResponse(new BaseResponse
            {
                HasError = true,
                Code = StatusCodes.Status500InternalServerError,
                Title = nameof(StatusMessage.Error),
                Message = exception.GetBaseException().Message
            });
        }
    }
}
