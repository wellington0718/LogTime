using LogTime.Api.CustomExceptionHandler;

namespace LogTime.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class StatusController(ILogTimeUnitOfWork logTimeUnitOfWork) : ApiControllerBase
    {
        [HttpPost]
        public async Task<ActionResult> Change(StatusHistoryChange statusHistoryChange)
        {
            try
            {
                var changeStatusResponse = await logTimeUnitOfWork.ChangeStatusAsync(statusHistoryChange.NewActivityId, statusHistoryChange.Id);
                return CreateResponse(changeStatusResponse);
            }
            catch (Exception ex)
            {
                throw LogTimeException.Throw(ex);
            }
        }
    }
}
