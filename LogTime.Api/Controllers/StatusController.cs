using LogTime.Api.Contracts;

namespace LogTime.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class StatusController(ILogTimeUnitOfWork logTimeUnitOfWork) : ApiControllerBase
    {
        [HttpPost]
        public async Task<ActionResult> Change(StatusChange statusChange)
        {
            try
            {
                var currentStatusHistory = await logTimeUnitOfWork.StatusHistoryRepository.FindAsync(statusChange.CurrentStatusHistoryId);
                var currentActiveLog = await logTimeUnitOfWork.ActiveLogRepository.FindAsync(activeLog => activeLog.ActualStatusHistoryId == currentStatusHistory.Id);

                if (currentActiveLog == null)
                {
                    return CreateResponse(new BaseResponse
                    {
                        Code = StatusCodes.Status200OK,
                        Title = "Ok",
                        IsSessionAlreadyClose = true
                    });
                }

                var logHistory = await logTimeUnitOfWork.LogHistoryRepository.FindAsync(currentActiveLog.ActualLogHistoryId);

                if (logHistory.LogoutDate.HasValue)
                {
                    await logTimeUnitOfWork.ActiveLogRepository.DeleteAsync(currentActiveLog);
                    await logTimeUnitOfWork.SaveChangesAsync();
                    await logTimeUnitOfWork.CommitAsync();

                    return CreateResponse(new BaseResponse
                    {
                        Code = StatusCodes.Status200OK,
                        Title = "Ok",
                        IsSessionAlreadyClose = true
                    });
                }

                var currentDateTime = DateTime.Now;

                logHistory.LastTimeConnectionAlive = currentDateTime;
                currentStatusHistory.StatusEndTime = currentDateTime;
                logTimeUnitOfWork.LogHistoryRepository.Update(logHistory);
                logTimeUnitOfWork.StatusHistoryRepository.Update(currentStatusHistory);

                var newStatusHistory = new StatusHistory
                {
                    LogId = currentStatusHistory.LogId,
                    StatusId = statusChange.NewActivityId,
                    StatusStartTime = currentDateTime
                };

                var createdStatusHistory = await logTimeUnitOfWork.StatusHistoryRepository.CreateAsync(newStatusHistory);
                await logTimeUnitOfWork.SaveChangesAsync();

                currentActiveLog.ActualStatusHistoryId = createdStatusHistory.Id;
                logTimeUnitOfWork.ActiveLogRepository.Update(currentActiveLog);

                await logTimeUnitOfWork.SaveChangesAsync();
                await logTimeUnitOfWork.CommitAsync();

                return CreateResponse(createdStatusHistory);
            }
            catch (Exception exception)
            {
                return CreateResponse(new BaseResponse
                {
                    HasError = true,
                    Code = StatusCodes.Status500InternalServerError,
                    Title = "Error",
                    Message = exception.GetBaseException().Message
                });
            }
        }
    }
}
