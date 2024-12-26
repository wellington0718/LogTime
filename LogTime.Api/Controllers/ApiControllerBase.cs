namespace LogTime.Api.Controllers;

public class ApiControllerBase : Controller
{
    protected ObjectResult CreateResponse<T>(T objectData)
    {
        return StatusCode(StatusCodes.Status200OK, objectData);
    }
}
