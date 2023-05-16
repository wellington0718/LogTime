using System;

namespace DataAccess.Models
{
    public class ActivityLog : BaseResponse
    {
        public int Id { get; set; }
        public int LogId { get; set; }
        public DateTime StatusStartTime { get; set; }
        public DateTime? StatusEndTime { get; set; }
        public int StatusId { get; set; }
    }
}
