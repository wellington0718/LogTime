using System;

namespace DataAccess.Models
{
    public class ActiveSession
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int ActualLogHistoryId { get; set; }
        public int ActualStatusHistoryId { get; set; }
        public int StatusId { get; set; }
        public DateTime StartDate { get; set; }
    }
}
