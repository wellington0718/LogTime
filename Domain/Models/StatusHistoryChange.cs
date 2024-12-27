namespace Domain.Models;

public class StatusHistoryChange : BaseResponse
{
    public int Id { get; set; }
    public int NewActivityId { get; set; }
    public DateTime StartTime { get; set; }
}
