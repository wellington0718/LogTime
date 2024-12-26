namespace Domain.Models;

public class EmployeeLeaveStatus : BaseResponse
{
    public int EmployeeId { get; set; }
    public string FullName { get; set; }
    public string Supervisor { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string ReturnTime { get; set; }
    public string Status { get; set; }
    public string Reason { get; set; }
    public bool IsNotAllowed { get; set; } = false;
}
