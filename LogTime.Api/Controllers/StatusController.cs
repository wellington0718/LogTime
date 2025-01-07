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
}
